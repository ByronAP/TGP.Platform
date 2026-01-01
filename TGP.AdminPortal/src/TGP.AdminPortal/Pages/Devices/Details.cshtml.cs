using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;

namespace TGP.AdminPortal.Pages.Devices;

public class DetailsModel : PageModel
{
    private readonly TgpDbContext _context;
    private readonly ILogger<DetailsModel> _logger;

    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string DeviceType { get; set; } = "";
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public bool IsOnline { get; set; }
    public int HeartbeatCount { get; set; }
    public List<MonitoredUserInfo> MonitoredUsers { get; set; } = new();
    public string? SuccessMessage { get; set; }

    public DetailsModel(TgpDbContext context, ILogger<DetailsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadDevice(id);
        return Id == Guid.Empty ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostRevokeEnrollmentAsync(Guid deviceId)
    {
        var device = await _context.Devices.FindAsync(deviceId);
        if (device != null)
        {
            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Revoked enrollment for device {DeviceId}", deviceId);
            return RedirectToPage("/Devices/Index");
        }
        await LoadDevice(deviceId);
        return Page();
    }

    private async Task LoadDevice(Guid id)
    {
        var device = await _context.Devices
            .Include(d => d.Tenant)
            .Include(d => d.MonitoredUsers)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (device == null) return;

        Id = device.Id;
        Name = device.DeviceName;
        DeviceType = device.DeviceType.ToString();
        TenantId = device.TenantId ?? Guid.Empty;
        TenantName = device.Tenant?.Name ?? "Unknown";
        CreatedAt = device.CreatedAt;
        LastHeartbeat = device.LastSeenAt;
        IsOnline = device.LastSeenAt.HasValue && (DateTime.UtcNow - device.LastSeenAt.Value).TotalMinutes < 5;

        HeartbeatCount = await _context.DeviceHeartbeats.CountAsync(h => h.DeviceId == id);

        MonitoredUsers = device.MonitoredUsers.Select(m => new MonitoredUserInfo
        {
            Username = m.Username,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public class MonitoredUserInfo
    {
        public string Username { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}
