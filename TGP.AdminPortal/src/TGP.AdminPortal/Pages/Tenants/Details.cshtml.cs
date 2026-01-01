using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Tenants;

public class DetailsModel : PageModel
{
    private readonly TgpDbContext _context;

    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string OwnerEmail { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public SubscriptionInfo? Subscription { get; set; }
    public List<MemberInfo> Members { get; set; } = new();
    public List<DeviceInfo> Devices { get; set; } = new();

    public DetailsModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var tenant = await _context.Tenants
            .Include(t => t.Owner)
            .Include(t => t.Members)
                .ThenInclude(m => m.User)
            .Include(t => t.Devices)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (tenant == null)
        {
            return NotFound();
        }

        // Query subscriptions separately
        var activeSub = await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.TenantId == id && s.Status == SubscriptionStatus.Active);

        Id = tenant.Id;
        Name = tenant.Name;
        OwnerEmail = tenant.Owner?.Email ?? "Unknown";
        CreatedAt = tenant.CreatedAt;
        UpdatedAt = tenant.UpdatedAt;

        if (activeSub != null)
        {
            Subscription = new SubscriptionInfo
            {
                Id = activeSub.Id,
                PlanName = activeSub.Plan?.Name ?? "Unknown",
                Status = activeSub.Status.ToString(),
                PeriodEnd = activeSub.CurrentPeriodEnd
            };
        }

        Members = tenant.Members.Select(m => new MemberInfo
        {
            Email = m.User?.Email ?? "Unknown",
            Role = m.Role,
            JoinedAt = m.JoinedAt
        }).ToList();

        Devices = tenant.Devices.Select(d => new DeviceInfo
        {
            Name = d.DeviceName,
            DeviceType = d.DeviceType.ToString(),
            LastHeartbeat = d.LastSeenAt
        }).ToList();

        return Page();
    }

    public class SubscriptionInfo
    {
        public Guid Id { get; set; }
        public string PlanName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime PeriodEnd { get; set; }
    }

    public class MemberInfo
    {
        public string Email { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime JoinedAt { get; set; }
    }

    public class DeviceInfo
    {
        public string Name { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public DateTime? LastHeartbeat { get; set; }
    }
}
