using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Plans;

public class IndexModel : PageModel
{
    private readonly TgpDbContext _context;

    public List<PlanViewModel> Plans { get; set; } = new();

    public IndexModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var plans = await _context.Plans
            .Include(p => p.Subscriptions)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Price)
            .ToListAsync();

        Plans = plans.Select(p => new PlanViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            YearlyPrice = p.YearlyPrice,
            Currency = p.Currency,
            Interval = p.Interval.ToString(),
            MaxDevices = p.MaxDevices,
            MaxProfiles = p.MaxProfiles,
            DataRetentionDays = p.DataRetentionDays,
            TrialDays = p.TrialDays,
            IsActive = p.IsActive,
            IsVisibleToPublic = p.IsVisibleToPublic,
            SortOrder = p.SortOrder,
            SubscriberCount = p.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active),
            // Stripe integration
            HasStripeProduct = !string.IsNullOrEmpty(p.StripeProductId),
            // Feature summary
            MonitoringFeatures = CountMonitoringFeatures(p),
            ControlFeatures = CountControlFeatures(p),
            ReportingFeatures = CountReportingFeatures(p)
        }).ToList();
    }

    private static int CountMonitoringFeatures(Plan p)
    {
        int count = 0;
        if (p.ProcessMonitoringAllowed) count++;
        if (p.BrowserMonitoringAllowed) count++;
        if (p.InputMonitoringAllowed) count++;
        if (p.ScreenCaptureAllowed) count++;
        if (p.LocationTrackingAllowed) count++;
        return count;
    }

    private static int CountControlFeatures(Plan p)
    {
        int count = 0;
        if (p.ScreenTimeAllowed) count++;
        if (p.AppBlockingAllowed) count++;
        if (p.WebFilteringAllowed) count++;
        if (p.CategoryFilteringAllowed) count++;
        return count;
    }

    private static int CountReportingFeatures(Plan p)
    {
        int count = 0;
        if (p.AlertsAllowed) count++;
        if (p.ReportingAllowed) count++;
        if (p.AppUsageReportsAllowed) count++;
        return count;
    }

    public class PlanViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? YearlyPrice { get; set; }
        public string Currency { get; set; } = "USD";
        public string Interval { get; set; } = "Month";
        public int MaxDevices { get; set; }
        public int MaxProfiles { get; set; }
        public int DataRetentionDays { get; set; }
        public int? TrialDays { get; set; }
        public bool IsActive { get; set; }
        public bool IsVisibleToPublic { get; set; }
        public int SortOrder { get; set; }
        public int SubscriberCount { get; set; }
        public bool HasStripeProduct { get; set; }
        // Feature counts for summary display
        public int MonitoringFeatures { get; set; }
        public int ControlFeatures { get; set; }
        public int ReportingFeatures { get; set; }
    }
}
