using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;

namespace TGP.AdminPortal.Pages.Monitoring;

public class IndexModel : PageModel
{
    private readonly TgpDbContext _context;

    public int TotalDevices { get; set; }
    public int OnlineDevices { get; set; }
    public int ActiveUsers { get; set; }
    public int HeartbeatsLast24h { get; set; }
    public Dictionary<string, int> DevicesByType { get; set; } = new();
    public List<HeartbeatInfo> RecentHeartbeats { get; set; } = new();
    public List<AuditLogInfo> RecentAuditLogs { get; set; } = new();

    public IndexModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var now = DateTime.UtcNow;
        var onlineThreshold = now.AddMinutes(-5);
        var last24h = now.AddHours(-24);

        // Device stats
        var devices = await _context.Devices.ToListAsync();
        TotalDevices = devices.Count;
        OnlineDevices = devices.Count(d => d.LastSeenAt.HasValue && d.LastSeenAt.Value > onlineThreshold);
        
        // Active users (users who logged in within last 7 days)
        var last7days = now.AddDays(-7);
        ActiveUsers = await _context.Users.CountAsync(u => u.LastLoginDate.HasValue && u.LastLoginDate.Value > last7days);

        // Heartbeats in last 24h
        HeartbeatsLast24h = await _context.DeviceHeartbeats.CountAsync(h => h.ReceivedAt > last24h);

        // Devices by type
        DevicesByType = devices
            .GroupBy(d => d.DeviceType)
            .ToDictionary(g => g.Key, g => g.Count());

        // Recent heartbeats
        var recentHeartbeats = await _context.DeviceHeartbeats
            .Include(h => h.Device)
                .ThenInclude(d => d.Tenant)
            .OrderByDescending(h => h.ReceivedAt)
            .Take(10)
            .ToListAsync();

        RecentHeartbeats = recentHeartbeats.Select(h => new HeartbeatInfo
        {
            DeviceName = h.Device?.DeviceName ?? "Unknown",
            TenantName = h.Device?.Tenant?.Name ?? "Unknown",
            ClientVersion = h.ClientVersion,
            ReceivedAt = h.ReceivedAt
        }).ToList();

        // Recent audit logs
        var recentLogs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToListAsync();

        var userIds = recentLogs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email ?? "Unknown");

        RecentAuditLogs = recentLogs.Select(l => new AuditLogInfo
        {
            Timestamp = l.Timestamp,
            Action = l.Action,
            EntityType = l.EntityType ?? "",
            UserEmail = l.UserId.HasValue && users.ContainsKey(l.UserId.Value) ? users[l.UserId.Value] : "System",
            Details = l.Details ?? ""
        }).ToList();
    }

    public class HeartbeatInfo
    {
        public string DeviceName { get; set; } = "";
        public string TenantName { get; set; } = "";
        public string ClientVersion { get; set; } = "";
        public DateTime ReceivedAt { get; set; }
    }

    public class AuditLogInfo
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = "";
        public string EntityType { get; set; } = "";
        public string UserEmail { get; set; } = "";
        public string Details { get; set; } = "";
    }
}
