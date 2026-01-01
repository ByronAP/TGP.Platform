using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;
using TGP.Data.Entities;

namespace TGP.AdminPortal.Pages.Users;

public class DetailsModel : PageModel
{
    private readonly TgpDbContext _context;
    private readonly ILogger<DetailsModel> _logger;

    public Guid Id { get; set; }
    public string Email { get; set; } = "";
    public bool EmailConfirmed { get; set; }
    public bool IsActive { get; set; }
    public bool MfaEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> AvailableRoles { get; set; } = new();
    public List<TenantMembershipInfo> TenantMemberships { get; set; } = new();
    public string? SuccessMessage { get; set; }

    public DetailsModel(TgpDbContext context, ILogger<DetailsModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        await LoadUser(id);
        return Id == Guid.Empty ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAddRoleAsync(Guid userId, string roleName)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role != null)
        {
            var existingUserRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == role.Id);
            
            if (existingUserRole == null)
            {
                _context.UserRoles.Add(new ApplicationUserRole { UserId = userId, RoleId = role.Id });
                await _context.SaveChangesAsync();
                _logger.LogInformation("Added role {Role} to user {UserId}", roleName, userId);
                SuccessMessage = $"Role '{roleName}' added successfully.";
            }
        }
        await LoadUser(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            // In a real implementation, this would trigger a password reset email
            user.SecurityStamp = Guid.NewGuid().ToString();
            await _context.SaveChangesAsync();
            _logger.LogInformation("Forced password reset for user {UserId}", userId);
            SuccessMessage = "Password reset initiated. User will be required to set a new password.";
        }
        await LoadUser(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostDisableMfaAsync(Guid userId)
    {
        var mfaConfigs = await _context.MfaConfigurations.Where(m => m.UserId == userId).ToListAsync();
        foreach (var config in mfaConfigs)
        {
            config.IsEnabled = false;
        }
        await _context.SaveChangesAsync();
        _logger.LogInformation("Disabled MFA for user {UserId}", userId);
        SuccessMessage = "MFA has been disabled for this user.";
        await LoadUser(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostDisableUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Disabled user {UserId}", userId);
            SuccessMessage = "User account has been disabled.";
        }
        await LoadUser(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostEnableUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = true;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Enabled user {UserId}", userId);
            SuccessMessage = "User account has been enabled.";
        }
        await LoadUser(userId);
        return Page();
    }

    private async Task LoadUser(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id);

        if (user == null) return;

        Id = user.Id;
        Email = user.Email ?? "";
        EmailConfirmed = user.EmailConfirmed;
        IsActive = user.IsActive;
        MfaEnabled = user.MfaEnabled;
        CreatedAt = user.CreatedAt;
        Roles = user.UserRoles.Select(ur => ur.Role?.Name ?? "").Where(r => !string.IsNullOrEmpty(r)).ToList();

        var allRoles = await _context.Roles.Select(r => r.Name).ToListAsync();
        AvailableRoles = allRoles.Where(r => !string.IsNullOrEmpty(r) && !Roles.Contains(r)).ToList()!;

        // Query tenant memberships separately
        var memberships = await _context.TenantMembers
            .Include(tm => tm.Tenant)
            .Where(tm => tm.UserId == id)
            .ToListAsync();

        TenantMemberships = memberships.Select(tm => new TenantMembershipInfo
        {
            TenantId = tm.TenantId,
            TenantName = tm.Tenant?.Name ?? "Unknown",
            Role = tm.Role,
            JoinedAt = tm.JoinedAt
        }).ToList();
    }

    public class TenantMembershipInfo
    {
        public Guid TenantId { get; set; }
        public string TenantName { get; set; } = "";
        public string Role { get; set; } = "";
        public DateTime JoinedAt { get; set; }
    }
}
