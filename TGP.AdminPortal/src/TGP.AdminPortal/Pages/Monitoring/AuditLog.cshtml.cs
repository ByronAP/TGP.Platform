using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;

namespace TGP.AdminPortal.Pages.Monitoring;

public class AuditLogModel : PageModel
{
    private readonly TgpDbContext _context;

    public List<AuditLogEntry> Logs { get; set; } = new();
    public List<string> AvailableActions { get; set; } = new();

    public AuditLogModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(500)
            .ToListAsync();

        var userIds = logs.Where(l => l.UserId.HasValue).Select(l => l.UserId!.Value).Distinct().ToList();
        var users = await _context.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email ?? "Unknown");

        Logs = logs.Select(l => new AuditLogEntry
        {
            Timestamp = l.Timestamp,
            Action = l.Action,
            EntityType = l.EntityType ?? "",
            EntityId = l.EntityId,
            UserEmail = l.UserId.HasValue && users.ContainsKey(l.UserId.Value) ? users[l.UserId.Value] : "System",
            IpAddress = l.IpAddress ?? "",
            Details = l.Details ?? ""
        }).ToList();

        AvailableActions = logs.Select(l => l.Action).Distinct().OrderBy(a => a).ToList();
    }

    public class AuditLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Action { get; set; } = "";
        public string EntityType { get; set; } = "";
        public Guid? EntityId { get; set; }
        public string UserEmail { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string Details { get; set; } = "";
    }
}
