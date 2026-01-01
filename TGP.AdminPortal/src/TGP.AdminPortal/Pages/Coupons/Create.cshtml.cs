using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Coupons;

public class CreateModel : PageModel
{
    private readonly TgpDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    [BindProperty]
    public string Code { get; set; } = "";

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public decimal? DiscountPercent { get; set; }

    [BindProperty]
    public decimal? DiscountAmount { get; set; }

    [BindProperty]
    public DateTime? ExpiresAt { get; set; }

    [BindProperty]
    public int? MaxRedemptions { get; set; }

    [BindProperty]
    public string? StripeCouponId { get; set; }

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
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "Coupon code is required.";
            return Page();
        }

        if (!DiscountPercent.HasValue && !DiscountAmount.HasValue)
        {
            ErrorMessage = "Either a discount percentage or amount is required.";
            return Page();
        }

        try
        {
            var coupon = new Coupon
            {
                Id = Guid.NewGuid(),
                Code = Code.Trim().ToUpperInvariant(),
                Description = Description?.Trim(),
                DiscountPercent = DiscountPercent,
                DiscountAmount = DiscountAmount,
                Currency = "USD",
                ExpiresAt = ExpiresAt,
                MaxRedemptions = MaxRedemptions,
                StripeCouponId = StripeCouponId?.Trim(),
                IsActive = true,
                CurrentRedemptions = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Coupons.Add(coupon);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created coupon {Code} with ID {CouponId}", coupon.Code, coupon.Id);
            return RedirectToPage("/Coupons/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating coupon {Code}", Code);
            ErrorMessage = "Failed to create coupon. The code may already exist.";
            return Page();
        }
    }
}
