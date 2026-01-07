using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.IgnoredApps;

public class CreateModel : PageModel
{
    private readonly TgpDbContext _context;
    private readonly ILogger<CreateModel> _logger;

    [BindProperty]
    public string ProcessName { get; set; } = "";

    [BindProperty]
    public string Platform { get; set; } = "Windows";

    [BindProperty]
    public string? Category { get; set; } = "System";

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public bool IsEnabled { get; set; } = true;

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
        if (string.IsNullOrWhiteSpace(ProcessName))
        {
            ErrorMessage = "Process name is required.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(Platform))
        {
            ErrorMessage = "Platform is required.";
            return Page();
        }

        try
        {
            var ignoredApp = new IgnoredApp
            {
                Id = Guid.NewGuid(),
                ProcessName = ProcessName.Trim(),
                Platform = Platform,
                Category = Category?.Trim(),
                Description = Description?.Trim(),
                IsEnabled = IsEnabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = null
            };

            _context.IgnoredApps.Add(ignoredApp);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created ignored app {ProcessName} for {Platform} with ID {Id}", 
                ignoredApp.ProcessName, ignoredApp.Platform, ignoredApp.Id);
            return RedirectToPage("/IgnoredApps/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ignored app {ProcessName}", ProcessName);
            ErrorMessage = "Failed to create ignored app. The process name may already exist for this platform.";
            return Page();
        }
    }
}
