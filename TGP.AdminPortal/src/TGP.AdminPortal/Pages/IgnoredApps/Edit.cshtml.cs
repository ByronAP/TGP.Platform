using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;

namespace TGP.AdminPortal.Pages.IgnoredApps;

public class EditModel : PageModel
{
    private readonly TgpDbContext _context;
    private readonly ILogger<EditModel> _logger;

    [BindProperty]
    public Guid Id { get; set; }

    [BindProperty]
    public string ProcessName { get; set; } = "";

    [BindProperty]
    public string Platform { get; set; } = "Windows";

    [BindProperty]
    public string? Category { get; set; }

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public bool IsEnabled { get; set; }

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public EditModel(TgpDbContext context, ILogger<EditModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        var app = await _context.IgnoredApps.FindAsync(id);
        if (app == null)
        {
            return NotFound();
        }

        Id = app.Id;
        ProcessName = app.ProcessName;
        Platform = app.Platform;
        Category = app.Category;
        Description = app.Description;
        IsEnabled = app.IsEnabled;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(ProcessName))
        {
            ErrorMessage = "Process name is required.";
            return Page();
        }

        var app = await _context.IgnoredApps.FindAsync(Id);
        if (app == null)
        {
            return NotFound();
        }

        try
        {
            app.ProcessName = ProcessName.Trim();
            app.Platform = Platform;
            app.Category = Category?.Trim();
            app.Description = Description?.Trim();
            app.IsEnabled = IsEnabled;
            app.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated ignored app {ProcessName} ({Id})", app.ProcessName, app.Id);
            SuccessMessage = "Ignored app updated successfully.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ignored app {Id}", Id);
            ErrorMessage = "Failed to update ignored app.";
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync()
    {
        var app = await _context.IgnoredApps.FindAsync(Id);
        if (app == null)
        {
            return NotFound();
        }

        try
        {
            _context.IgnoredApps.Remove(app);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted ignored app {ProcessName} ({Id})", app.ProcessName, app.Id);
            return RedirectToPage("/IgnoredApps/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ignored app {Id}", Id);
            ErrorMessage = "Failed to delete ignored app.";
            return Page();
        }
    }
}
