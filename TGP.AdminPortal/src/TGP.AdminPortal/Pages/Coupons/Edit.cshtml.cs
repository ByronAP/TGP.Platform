using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;

namespace TGP.AdminPortal.Pages.Coupons;

public class EditModel : PageModel
{
    private readonly TgpDbContext _context;
    private readonly ILogger<EditModel> _logger;

    [BindProperty]
    public Guid Id { get; set; }

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
    public bool IsActive { get; set; }

    public int CurrentRedemptions { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public EditModel(TgpDbContext context, ILogger<EditModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var coupon = await _context.Coupons.FindAsync(id);
        if (coupon == null)
        {
            return NotFound();
        }

        LoadCoupon(coupon);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var coupon = await _context.Coupons.FindAsync(Id);
        if (coupon == null)
        {
            return NotFound();
        }

        try
        {
            coupon.Code = Code.Trim().ToUpperInvariant();
            coupon.Description = Description?.Trim();
            coupon.DiscountPercent = DiscountPercent;
            coupon.DiscountAmount = DiscountAmount;
            coupon.ExpiresAt = ExpiresAt;
            coupon.MaxRedemptions = MaxRedemptions;
            coupon.IsActive = IsActive;
            coupon.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated coupon {CouponId}", Id);
            SuccessMessage = "Coupon updated successfully.";
            LoadCoupon(coupon);
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating coupon {CouponId}", Id);
            ErrorMessage = "Failed to update coupon.";
            LoadCoupon(coupon);
            return Page();
        }
    }

    private void LoadCoupon(TGP.Data.Entities.Coupon coupon)
    {
        Id = coupon.Id;
        Code = coupon.Code;
        Description = coupon.Description;
        DiscountPercent = coupon.DiscountPercent;
        DiscountAmount = coupon.DiscountAmount;
        ExpiresAt = coupon.ExpiresAt;
        MaxRedemptions = coupon.MaxRedemptions;
        IsActive = coupon.IsActive;
        CurrentRedemptions = coupon.CurrentRedemptions;
    }
}
