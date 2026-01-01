using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;

namespace TGP.AdminPortal.Pages.Coupons;

public class IndexModel : PageModel
{
    private readonly TgpDbContext _context;

    public List<CouponViewModel> Coupons { get; set; } = new();

    public IndexModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var coupons = await _context.Coupons
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        Coupons = coupons.Select(c => new CouponViewModel
        {
            Id = c.Id,
            Code = c.Code,
            Description = c.Description,
            DiscountPercent = c.DiscountPercent,
            DiscountAmount = c.DiscountAmount,
            ExpiresAt = c.ExpiresAt,
            MaxRedemptions = c.MaxRedemptions,
            CurrentRedemptions = c.CurrentRedemptions,
            IsActive = c.IsActive
        }).ToList();
    }

    public class CouponViewModel
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string? Description { get; set; }
        public decimal? DiscountPercent { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? MaxRedemptions { get; set; }
        public int CurrentRedemptions { get; set; }
        public bool IsActive { get; set; }
    }
}
