using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TGP.AdminPortal.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoginModel> _logger;

    [BindProperty]
    public string Email { get; set; } = "";

    [BindProperty]
    public string Password { get; set; } = "";

    [BindProperty]
    public bool RememberMe { get; set; }

    public string? ErrorMessage { get; set; }

    public LoginModel(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<LoginModel> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/Dashboard");

        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Please enter your email and password.";
            return Page();
        }

        try
        {
            // Call SSO service to authenticate
            var client = _httpClientFactory.CreateClient("SSO");
            var loginRequest = new
            {
                Username = Email, // API expects Username, but we use Email as username
                Password = Password
            };

            var response = await client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                
                if (result == null)
                {
                    ErrorMessage = "Invalid response from authentication service.";
                    return Page();
                }

                // Check if user has admin role
                if (!result.User.Roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                                           r.Equals("SystemAdmin", StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogWarning("User {Email} attempted admin login without admin role", Email);
                    ErrorMessage = "Access denied. Administrator privileges required.";
                    return Page();
                }

                // Create claims for the authenticated admin
                var claims = new List<Claim>
                {
                    new(ClaimTypes.NameIdentifier, result.User.Id),
                    new(ClaimTypes.Email, Email),
                    new(ClaimTypes.Name, result.User.Email ?? Email),
                    new("access_token", result.AccessToken ?? ""),
                    new("tenant_id", "") // SSO response doesn't provide tenant_id in User object currently
                };

                // Add role claims
                foreach (var role in result.User.Roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = RememberMe,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(RememberMe ? 24 : 8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                _logger.LogInformation("Admin user {Email} logged in successfully", Email);
                return LocalRedirect(returnUrl);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Login failed for {Email}: {StatusCode} - {Error}", 
                    Email, response.StatusCode, errorContent);
                ErrorMessage = "Invalid email or password.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for {Email}", Email);
            ErrorMessage = "Unable to connect to authentication service. Please try again.";
            return Page();
        }
    }

    private class LoginResponse
    {
        public string AccessToken { get; set; } = "";
        public string RefreshToken { get; set; } = "";
        public int ExpiresIn { get; set; }
        public string TokenType { get; set; } = "";
        public UserResponse User { get; set; } = new();
        public string? TenantStatus { get; set; }
    }

    private class UserResponse
    {
        public string Id { get; set; } = "";
        public string Username { get; set; } = "";
        public string Email { get; set; } = "";
        public List<string> Roles { get; set; } = new();
    }
}
