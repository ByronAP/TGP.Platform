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

    [BindProperty]
    public decimal Price { get; set; }

    [BindProperty]
    public string Interval { get; set; } = "Month";

    [BindProperty]
    public int MaxDevices { get; set; }

    [BindProperty]
    public string? StripePriceId { get; set; }

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

        Id = plan.Id;
        Name = plan.Name;
        Description = plan.Description;
        Price = plan.Price;
        Interval = plan.Interval.ToString();
        MaxDevices = plan.MaxDevices;

        ActiveSubscriberCount = plan.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active);
        TotalSubscriberCount = plan.Subscriptions.Count;

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
            await LoadStats(plan);
            return Page();
        }

        try
        {
            plan.Name = Name.Trim();
            plan.Description = Description?.Trim();
            plan.Price = Price;
            plan.Interval = Interval == "Year" ? PlanInterval.Year : PlanInterval.Month;
            plan.MaxDevices = MaxDevices;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated plan {PlanId}", plan.Id);
            SuccessMessage = "Plan updated successfully.";
            await LoadStats(plan);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating plan {PlanId}", Id);
            ErrorMessage = "Failed to update plan. Please try again.";
            await LoadStats(plan);
            return Page();
        }
    }

    private Task LoadStats(Plan plan)
    {
        ActiveSubscriberCount = plan.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active);
        TotalSubscriberCount = plan.Subscriptions.Count;
        return Task.CompletedTask;
    }
}
