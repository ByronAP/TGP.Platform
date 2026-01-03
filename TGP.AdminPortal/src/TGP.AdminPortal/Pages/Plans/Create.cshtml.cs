using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Plans;

public class CreateModel : PageModel
{
    private readonly TgpDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    [BindProperty]
    public string Name { get; set; } = "";

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public decimal Price { get; set; } = 9.99m;

    [BindProperty]
    public string Interval { get; set; } = "Month";

    [BindProperty]
    public int MaxDevices { get; set; } = 10;

    [BindProperty]
    public decimal MaxTotalStorageGB { get; set; } = 5;

    [BindProperty]
    public string? StripePriceId { get; set; }

    public string? ErrorMessage { get; set; }

    public CreateModel(TgpDbContext context, ILogger<CreateModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ErrorMessage = "Plan name is required.";
            return Page();
        }

        if (Price < 0)
        {
            ErrorMessage = "Price cannot be negative.";
            return Page();
        }

        if (MaxDevices < 1)
        {
            ErrorMessage = "Max devices must be at least 1.";
            return Page();
        }

        try
        {
            var plan = new Plan
            {
                Id = Guid.NewGuid(),
                Name = Name.Trim(),
                Description = Description?.Trim(),
                Price = Price,
                Currency = "USD",
                Interval = Interval == "Year" ? PlanInterval.Year : PlanInterval.Month,
                MaxDevices = MaxDevices,
                MaxTotalStorageBytes = (long)(MaxTotalStorageGB * 1_073_741_824)
            };

            _context.Plans.Add(plan);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created plan {PlanName} with ID {PlanId}", plan.Name, plan.Id);
            return RedirectToPage("/Plans/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating plan {PlanName}", Name);
            ErrorMessage = "Failed to create plan. Please try again.";
            return Page();
        }
    }
}
