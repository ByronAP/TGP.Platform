using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Subscriptions;

public class IndexModel : PageModel
{
    private readonly TgpDbContext _context;

    public List<SubscriptionViewModel> Subscriptions { get; set; } = new();

    public IndexModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var subscriptions = await _context.Subscriptions
            .Include(s => s.Tenant)
            .Include(s => s.Plan)
            .OrderByDescending(s => s.CurrentPeriodEnd)
            .ToListAsync();

        Subscriptions = subscriptions.Select(s => new SubscriptionViewModel
        {
            Id = s.Id,
            TenantId = s.TenantId,
            TenantName = s.Tenant?.Name ?? "Unknown",
            PlanId = s.PlanId,
            PlanName = s.Plan?.Name ?? "Unknown",
            Status = s.Status.ToString(),
            PeriodStart = s.CurrentPeriodStart,
            PeriodEnd = s.CurrentPeriodEnd,
            Amount = s.Plan?.Price ?? 0,
            Interval = s.Plan?.Interval == PlanInterval.Year ? "year" : "month"
        }).ToList();
    }

    public class SubscriptionViewModel
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = "";
        public Guid PlanId { get; set; }
        public string PlanName { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
        public decimal Amount { get; set; }
        public string Interval { get; set; } = "month";
    }
}
