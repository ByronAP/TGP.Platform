using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Subscriptions;

public class EditModel : PageModel
{
    private readonly TgpDbContext _context;
    private readonly ILogger<EditModel> _logger;

    [BindProperty]
    public Guid Id { get; set; }

    public string TenantName { get; set; } = "";
    public string Status { get; set; } = "";
    public string CurrentPlanName { get; set; } = "";
    public Guid CurrentPlanId { get; set; }
    public decimal Amount { get; set; }
    public string Interval { get; set; } = "month";
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    [BindProperty]
    public Guid NewPlanId { get; set; }

    [BindProperty]
    public string NewStatus { get; set; } = "";

    [BindProperty]
    public int ExtendDays { get; set; }

    public List<PlanOption> AvailablePlans { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public EditModel(TgpDbContext context, ILogger<EditModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subscription == null)
        {
            return NotFound();
        }

        await LoadSubscription(subscription);
        await LoadPlans();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Tenant)
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == Id);

        if (subscription == null)
        {
            return NotFound();
        }

        try
        {
            var changes = new List<string>();

            // Update plan if changed
            if (NewPlanId != Guid.Empty && NewPlanId != subscription.PlanId)
            {
                var newPlan = await _context.Plans.FindAsync(NewPlanId);
                if (newPlan != null)
                {
                    subscription.PlanId = NewPlanId;
                    changes.Add($"Plan changed to {newPlan.Name}");
                }
            }

            // Update status if changed
            if (!string.IsNullOrEmpty(NewStatus) && Enum.TryParse<SubscriptionStatus>(NewStatus, out var status))
            {
                if (subscription.Status != status)
                {
                    subscription.Status = status;
                    changes.Add($"Status changed to {status}");
                }
            }

            // Extend billing period
            if (ExtendDays > 0)
            {
                subscription.CurrentPeriodEnd = subscription.CurrentPeriodEnd.AddDays(ExtendDays);
                changes.Add($"Billing period extended by {ExtendDays} days");
            }

            if (changes.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Updated subscription {SubscriptionId}: {Changes}", Id, string.Join(", ", changes));
                SuccessMessage = string.Join(". ", changes) + ".";
            }

            // Reload data
            subscription = await _context.Subscriptions
                .Include(s => s.Tenant)
                .Include(s => s.Plan)
                .FirstOrDefaultAsync(s => s.Id == Id);
            
            if (subscription != null)
            {
                await LoadSubscription(subscription);
            }
            await LoadPlans();
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating subscription {SubscriptionId}", Id);
            ErrorMessage = "Failed to update subscription.";
            await LoadSubscription(subscription);
            await LoadPlans();
            return Page();
        }
    }

    private Task LoadSubscription(Subscription subscription)
    {
        Id = subscription.Id;
        TenantName = subscription.Tenant?.Name ?? "Unknown";
        Status = subscription.Status.ToString();
        CurrentPlanName = subscription.Plan?.Name ?? "Unknown";
        CurrentPlanId = subscription.PlanId;
        Amount = subscription.Plan?.Price ?? 0;
        Interval = subscription.Plan?.Interval == PlanInterval.Year ? "year" : "month";
        PeriodStart = subscription.CurrentPeriodStart;
        PeriodEnd = subscription.CurrentPeriodEnd;
        return Task.CompletedTask;
    }

    private async Task LoadPlans()
    {
        var plans = await _context.Plans.OrderBy(p => p.Price).ToListAsync();
        AvailablePlans = plans.Select(p => new PlanOption
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price,
            Interval = p.Interval.ToString()
        }).ToList();
    }

    public class PlanOption
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Interval { get; set; } = "Month";
    }
}
