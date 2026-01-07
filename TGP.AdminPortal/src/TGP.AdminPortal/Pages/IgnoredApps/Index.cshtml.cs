using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;

namespace TGP.AdminPortal.Pages.IgnoredApps;

public class IndexModel : PageModel
{
    private readonly TgpDbContext _context;

    public List<IgnoredAppViewModel> IgnoredApps { get; set; } = new();
    public string? FilterPlatform { get; set; }
    public string? FilterCategory { get; set; }

    public IndexModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync(string? platform = null, string? category = null)
    {
        FilterPlatform = platform;
        FilterCategory = category;

        var query = _context.IgnoredApps.AsQueryable();

        if (!string.IsNullOrEmpty(platform))
        {
            query = query.Where(a => a.Platform == platform);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(a => a.Category == category);
        }

        var apps = await query
            .OrderBy(a => a.Platform)
            .ThenBy(a => a.Category)
            .ThenBy(a => a.ProcessName)
            .ToListAsync();

        IgnoredApps = apps.Select(a => new IgnoredAppViewModel
        {
            Id = a.Id,
            Platform = a.Platform,
            ProcessName = a.ProcessName,
            Category = a.Category,
            Description = a.Description,
            IsEnabled = a.IsEnabled,
            CreatedAt = a.CreatedAt
        }).ToList();
    }

    public class IgnoredAppViewModel
    {
        public Guid Id { get; set; }
        public string Platform { get; set; } = "";
        public string ProcessName { get; set; } = "";
        public string? Category { get; set; }
        public string? Description { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
