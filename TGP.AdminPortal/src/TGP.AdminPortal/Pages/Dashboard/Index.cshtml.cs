using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Dashboard;

public class IndexModel : PageModel
{
    private readonly TgpDbContext _context;

    public int TotalTenants { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int TotalDevices { get; set; }
    public List<PlanStat> PlanStats { get; set; } = new();
    public List<ActivityItem> RecentActivity { get; set; } = new();

    public IndexModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        TotalTenants = await _context.Tenants.CountAsync();
        TotalDevices = await _context.Devices.CountAsync();

        // Get subscription stats
        var subscriptions = await _context.Subscriptions
            .Include(s => s.Plan)
            .ToListAsync();

        ActiveSubscriptions = subscriptions.Count(s => s.Status == SubscriptionStatus.Active);

        // Calculate MRR
        MonthlyRevenue = subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active && s.Plan != null)
            .Sum(s => s.Plan!.Interval == PlanInterval.Year 
                ? s.Plan.Price / 12 
                : s.Plan.Price);

        // Plan stats
        var plans = await _context.Plans.ToListAsync();
        foreach (var plan in plans.OrderBy(p => p.Price))
        {
            var planSubs = subscriptions.Where(s => s.PlanId == plan.Id).ToList();
            PlanStats.Add(new PlanStat
            {
                Name = plan.Name,
                ActiveCount = planSubs.Count(s => s.Status == SubscriptionStatus.Active),
                TrialingCount = planSubs.Count(s => s.Status == SubscriptionStatus.Trialing),
                PastDueCount = planSubs.Count(s => s.Status == SubscriptionStatus.PastDue),
                Revenue = planSubs
                    .Where(s => s.Status == SubscriptionStatus.Active)
                    .Sum(_ => plan.Interval == PlanInterval.Year 
                        ? plan.Price / 12 
                        : plan.Price)
            });
        }

        // Recent audit activity
        var recentLogs = await _context.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(10)
            .ToListAsync();

        RecentActivity = recentLogs.Select(a => new ActivityItem
        {
            Timestamp = a.Timestamp,
            Description = a.Action,
            UserEmail = a.UserId?.ToString() ?? "System"
        }).ToList();
    }

    public class PlanStat
    {
        public string Name { get; set; } = "";
        public int ActiveCount { get; set; }
        public int TrialingCount { get; set; }
        public int PastDueCount { get; set; }
        public decimal Revenue { get; set; }
    }

    public class ActivityItem
    {
        public DateTime Timestamp { get; set; }
        public string Description { get; set; } = "";
        public string UserEmail { get; set; } = "";
    }
}
