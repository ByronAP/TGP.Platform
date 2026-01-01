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
            .OrderBy(p => p.Price)
            .ToListAsync();

        Plans = plans.Select(p => new PlanViewModel
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            Currency = p.Currency,
            Interval = p.Interval.ToString(),
            MaxDevices = p.MaxDevices,
            SubscriberCount = p.Subscriptions.Count(s => s.Status == SubscriptionStatus.Active)
        }).ToList();
    }

    public class PlanViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public string Interval { get; set; } = "Month";
        public int MaxDevices { get; set; }
        public int SubscriberCount { get; set; }
    }
}
