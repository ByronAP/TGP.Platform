using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TGP.Data;

namespace TGP.AdminPortal.Pages.Users;

public class IndexModel : PageModel
{
    private readonly TgpDbContext _context;

    public List<UserViewModel> Users { get; set; } = new();

    public IndexModel(TgpDbContext context)
    {
        _context = context;
    }

    public async Task OnGetAsync()
    {
        var users = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        Users = users.Select(u => new UserViewModel
        {
            Id = u.Id,
            Email = u.Email ?? "",
            Roles = u.UserRoles.Select(ur => ur.Role?.Name ?? "").Where(r => !string.IsNullOrEmpty(r)).ToList(),
            EmailConfirmed = u.EmailConfirmed,
            MfaEnabled = u.MfaEnabled,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        }).ToList();
    }

    public class UserViewModel
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = "";
        public List<string> Roles { get; set; } = new();
        public bool EmailConfirmed { get; set; }
        public bool MfaEnabled { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
