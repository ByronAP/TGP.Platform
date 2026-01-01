using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Tenants;

public class IndexModel : PageModel
{
    private readonly TgpDbContext _context;

    public List<TenantViewModel> Tenants { get; set; } = new();

    public IndexModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var tenants = await _context.Tenants
            .Include(t => t.Owner)
            .Include(t => t.Members)
            .Include(t => t.Devices)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        // Get all subscriptions separately
        var subscriptions = await _context.Subscriptions
            .Include(s => s.Plan)
            .ToListAsync();
        var subscriptionsByTenant = subscriptions
            .GroupBy(s => s.TenantId)
            .ToDictionary(g => g.Key, g => g.ToList());

        Tenants = tenants.Select(t =>
        {
            var tenantSubs = subscriptionsByTenant.GetValueOrDefault(t.Id) ?? new List<TGP.Data.Entities.Subscription>();
            var activeSub = tenantSubs.FirstOrDefault(s => s.Status == SubscriptionStatus.Active);
            return new TenantViewModel
            {
                Id = t.Id,
                Name = t.Name,
                OwnerEmail = t.Owner?.Email ?? "Unknown",
                MemberCount = t.Members.Count,
                DeviceCount = t.Devices.Count,
                HasActiveSubscription = activeSub != null,
                PlanName = activeSub?.Plan?.Name ?? "",
                CreatedAt = t.CreatedAt
            };
        }).ToList();
    }

    public class TenantViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string OwnerEmail { get; set; } = "";
        public int MemberCount { get; set; }
        public int DeviceCount { get; set; }
        public bool HasActiveSubscription { get; set; }
        public string PlanName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
