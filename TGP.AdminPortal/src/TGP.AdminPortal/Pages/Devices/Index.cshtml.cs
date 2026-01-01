using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;

namespace TGP.AdminPortal.Pages.Devices;

public class IndexModel : PageModel
{
    private readonly TgpDbContext _context;

    public List<DeviceViewModel> Devices { get; set; } = new();

    public IndexModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var devices = await _context.Devices
            .Include(d => d.Tenant)
            .OrderByDescending(d => d.LastSeenAt)
            .ToListAsync();

        Devices = devices.Select(d => new DeviceViewModel
        {
            Id = d.Id,
            Name = d.DeviceName,
            DeviceType = d.DeviceType.ToString(),
            TenantId = d.TenantId ?? Guid.Empty,
            TenantName = d.Tenant?.Name ?? "Unknown",
            LastHeartbeat = d.LastSeenAt,
            IsOnline = d.LastSeenAt.HasValue && (DateTime.UtcNow - d.LastSeenAt.Value).TotalMinutes < 5
        }).ToList();
    }

    public class DeviceViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string DeviceType { get; set; } = "";
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = "";
        public DateTime? LastHeartbeat { get; set; }
        public bool IsOnline { get; set; }
    }
}
