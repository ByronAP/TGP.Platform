using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Plans;

public class EditModel : PageModel
{
    private readonly TgpDbContext _context;
    private readonly ILogger<EditModel> _logger;

    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public string Name { get; set; } = "";

    [BindProperty]
    public string? Description { get; set; }

    // Pricing
    [BindProperty]
    public decimal Price { get; set; }

    [BindProperty]
    public decimal? YearlyPrice { get; set; }

    [BindProperty]
    public string Interval { get; set; } = "Month";

    // Limits
    [BindProperty]
    public int MaxDevices { get; set; }

    [BindProperty]
    public int MaxProfiles { get; set; }

    [BindProperty]
    public int DataRetentionDays { get; set; }

    [BindProperty]
    public decimal MaxTotalStorageGB { get; set; }

    // Trial
    [BindProperty]
    public int? TrialDays { get; set; }

    // Plan availability
    [BindProperty]
    public bool IsActive { get; set; }

    [BindProperty]
    public bool IsVisibleToPublic { get; set; }

    [BindProperty]
    public int SortOrder { get; set; }

    // Feature flags - Monitoring
    [BindProperty]
    public bool ProcessMonitoringAllowed { get; set; }

    [BindProperty]
    public bool BrowserMonitoringAllowed { get; set; }

    [BindProperty]
    public bool InputMonitoringAllowed { get; set; }

    [BindProperty]
    public bool ScreenCaptureAllowed { get; set; }

    [BindProperty]
    public bool LocationTrackingAllowed { get; set; }

    // Feature flags - Controls
    [BindProperty]
    public bool ScreenTimeAllowed { get; set; }

    [BindProperty]
    public bool AppBlockingAllowed { get; set; }

    [BindProperty]
    public bool WebFilteringAllowed { get; set; }

    [BindProperty]
    public bool CategoryFilteringAllowed { get; set; }

    // Feature flags - Reporting & Alerts
    [BindProperty]
    public bool AlertsAllowed { get; set; }

    [BindProperty]
    public bool ReportingAllowed { get; set; }

    [BindProperty]
    public bool AppUsageReportsAllowed { get; set; }

    // Stripe (read-only display)
    public string? StripeProductId { get; set; }
    public string? StripeMonthlyPriceId { get; set; }
    public string? StripeYearlyPriceId { get; set; }

    public int ActiveSubscriberCount { get; set; }
    public int TotalSubscriberCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public EditModel(TgpDbContext context, ILogger<EditModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var plan = await _context.Plans
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (plan == null)
        {
            return NotFound();
        }

        LoadPlanData(plan);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var plan = await _context.Plans
            .Include(p => p.Subscriptions)
            .FirstOrDefaultAsync(p => p.Id == Id);

        if (plan == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Plan name is required.";
            LoadPlanData(plan);
            return Page();
        }

        try
        {
            // Basic info
            plan.Name = Name.Trim();
            plan.Description = Description?.Trim();
            
            // Pricing
            plan.Price = Price;
            plan.YearlyPrice = YearlyPrice;
            plan.Interval = Interval == "Year" ? PlanInterval.Year : PlanInterval.Month;
            
            // Limits
            plan.MaxDevices = MaxDevices;
            plan.MaxProfiles = MaxProfiles;
            plan.DataRetentionDays = DataRetentionDays;
            plan.MaxTotalStorageBytes = (long)(MaxTotalStorageGB * 1_073_741_824); // GB to bytes
            plan.TrialDays = TrialDays;
            
            // Availability
            plan.IsActive = IsActive;
            plan.IsVisibleToPublic = IsVisibleToPublic;
            plan.SortOrder = SortOrder;
            
            // Feature flags - Monitoring
            plan.ProcessMonitoringAllowed = ProcessMonitoringAllowed;
            plan.BrowserMonitoringAllowed = BrowserMonitoringAllowed;
            plan.InputMonitoringAllowed = InputMonitoringAllowed;
            plan.ScreenCaptureAllowed = ScreenCaptureAllowed;
            plan.LocationTrackingAllowed = LocationTrackingAllowed;
            
            // Feature flags - Controls
            plan.ScreenTimeAllowed = ScreenTimeAllowed;
            plan.AppBlockingAllowed = AppBlockingAllowed;
            plan.WebFilteringAllowed = WebFilteringAllowed;
            plan.CategoryFilteringAllowed = CategoryFilteringAllowed;
            
            // Feature flags - Reporting & Alerts
            plan.AlertsAllowed = AlertsAllowed;
            plan.ReportingAllowed = ReportingAllowed;
            plan.AppUsageReportsAllowed = AppUsageReportsAllowed;
            
            plan.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated plan {PlanId}", plan.Id);
            SuccessMessage = "Plan updated successfully.";
            LoadPlanData(plan);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan {PlanId}", Id);
            ErrorMessage = "Failed to update plan. Please try again.";
            LoadPlanData(plan);
            return Page();
        }
    }

    private void LoadPlanData(Plan plan)
    {
        Id = plan.Id;
        Name = plan.Name;
        Description = plan.Description;
        
        // Pricing
        Price = plan.Price;
        YearlyPrice = plan.YearlyPrice;
        Interval = plan.Interval.ToString();
        
        // Limits
        MaxDevices = plan.MaxDevices;
        MaxProfiles = plan.MaxProfiles;
        DataRetentionDays = plan.DataRetentionDays;
        MaxTotalStorageGB = Math.Round((decimal)plan.MaxTotalStorageBytes / 1_073_741_824, 2); // bytes to GB
        TrialDays = plan.TrialDays;
        
        // Availability
        IsActive = plan.IsActive;
        IsVisibleToPublic = plan.IsVisibleToPublic;
        SortOrder = plan.SortOrder;
        
        // Feature flags - Monitoring
        ProcessMonitoringAllowed = plan.ProcessMonitoringAllowed;
        BrowserMonitoringAllowed = plan.BrowserMonitoringAllowed;
        InputMonitoringAllowed = plan.InputMonitoringAllowed;
        ScreenCaptureAllowed = plan.ScreenCaptureAllowed;
        LocationTrackingAllowed = plan.LocationTrackingAllowed;
        
        // Feature flags - Controls
        ScreenTimeAllowed = plan.ScreenTimeAllowed;
        AppBlockingAllowed = plan.AppBlockingAllowed;
        WebFilteringAllowed = plan.WebFilteringAllowed;
        CategoryFilteringAllowed = plan.CategoryFilteringAllowed;
        
        // Feature flags - Reporting & Alerts
        AlertsAllowed = plan.AlertsAllowed;
        ReportingAllowed = plan.ReportingAllowed;
        AppUsageReportsAllowed = plan.AppUsageReportsAllowed;
        
        // Stripe (read-only)
        StripeProductId = plan.StripeProductId;
        StripeMonthlyPriceId = plan.StripeMonthlyPriceId;
        StripeYearlyPriceId = plan.StripeYearlyPriceId;

        ActiveSubscriberCount = plan.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active);
        TotalSubscriberCount = plan.Subscriptions.Count;
    }
}
