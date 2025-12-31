using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TGP.TestDataGenerator.Services;

public record SeededAgent(string DeviceId, string Token, string Type);

public class DataSeeder
{
    private readonly IConfiguration _config;
    private readonly ILogger<DataSeeder> _logger;
    private readonly HttpClient _ssoClient;

    public DataSeeder(IConfiguration config, ILogger<DataSeeder> logger)
    {
        _config = config;
        _logger = logger;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _ssoClient = new HttpClient(handler) { BaseAddress = new Uri(_config["Sso:Url"]!) };
    }

    public async Task<List<SeededAgent>> SeedAsync(int userCount, int devicesPerUser, bool dbOnly = false)
    {
        _logger.LogInformation($"Seeding {userCount} users with {devicesPerUser} devices each via API...");

        var agents = new List<SeededAgent>();

        for (int i = 0; i < userCount; i++)
        {
            var username = $"user_{i}";
            var password = "TestUser!123456";
            var email = $"user_{i}@test.local";

            _logger.LogInformation($"Processing user {username}...");

            string? userToken = await LoginUserAsync(username, password);

            if (userToken == null)
            {
                _logger.LogInformation($"User {username} not found or login failed. Attempting registration...");
                if (await RegisterUserAsync(username, email, password))
                {
                    userToken = await LoginUserAsync(username, password);
                }
            }

            if (userToken == null)
            {
                _logger.LogError($"Failed to authenticate user {username}. Skipping.");
                continue;
            }

            // Decode token to get TenantId if possible, or assume user default intent
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(userToken);
            var tenantIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tid" || c.Type == "TenantId");
            
            // If API doesn't return TenantId in token (it usually does), we might be blocked from creating devices strictly.
            // But RegisterDeviceAsync usually accepts TenantId. 
            // If SSO token has TenantId, use it.
            Guid? tenantId = tenantIdClaim != null ? Guid.Parse(tenantIdClaim.Value) : null;

            if (tenantId == null)
            {
                 // Try to see if login response gave us it?
                 _logger.LogWarning("No TenantId found in token. Using empty (User intent).");
            }

            for (int d = 0; d < devicesPerUser; d++)
            {
                var deviceId = Guid.NewGuid().ToString(); // We generate it
                var deviceName = $"Device_{i}_{d}";
                var deviceType = d % 2 == 0 ? "Desktop" : "Android";

                var deviceToken = await RegisterDeviceAsync(userToken, deviceId, deviceName, deviceType, tenantId);
                if (deviceToken != null)
                {
                    agents.Add(new SeededAgent(deviceId, deviceToken, deviceType));
                }
            }
        }

        return agents;
    }

    public async Task<(string? userToken, string? deviceToken, string? deviceId, Guid? userId, Guid? tenantId)> EnsureTestUserEnvironmentAsync()
    {
        var username = "test@tgp.local";
        var password = "User123!";
        var email = "test@tgp.local";
        var deviceIdStr = "desktop_test_01"; 

        _logger.LogInformation($"Ensuring test environment for '{username}' via API...");

        // 1. Strict Login
        string? userToken = await LoginUserAsync(username, password);
        Guid? userId = null;
        Guid? tenantId = null;

        if (userToken == null)
        {
            _logger.LogError($"Login failed for user '{username}'. Please verify the password in DataSeeder.cs or the database. Auto-registration is disabled.");
            return (null, null, null, null, null);
        }

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(userToken);
        var uidStr = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid")?.Value;
        var tidStr = jwt.Claims.FirstOrDefault(c => c.Type == "tid")?.Value;

        if (uidStr != null) userId = Guid.Parse(uidStr);
        if (tidStr != null) tenantId = Guid.Parse(tidStr);

        // 2. Register Test Device (or get token if exists)
        _logger.LogInformation($"Registering device '{deviceIdStr}'...");
        var deviceToken = await RegisterDeviceAsync(userToken, deviceIdStr, "Test Windows PC", "Desktop", tenantId);
        
        if (deviceToken == null)
        {
            _logger.LogWarning($"Device registration failed (likely exists). Attempting to login explicitly with DeviceId to get device-bound token.");
            
            // Fallback: Login again specifying the DeviceId to get a token with 'device_id' claim
            deviceToken = await LoginUserAsync(username, password, deviceIdStr);

            if (deviceToken == null)
            {
                 _logger.LogError("Failed to obtain device token via Login fallback.");
                 return (userToken, null, deviceIdStr, userId, tenantId);
            }
             _logger.LogInformation("Successfully obtained device-bound token via Login.");
        }

        return (userToken, deviceToken, deviceIdStr, userId, tenantId);
    }

    private async Task<string?> LoginUserAsync(string username, string password, string? deviceId = null)
    {
        try
        {
            var response = await _ssoClient.PostAsJsonAsync("/api/v1/Auth/login", new
            {
                Username = username,
                Password = password,
                RememberMe = false,
                DeviceId = deviceId
            });

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                return result?.AccessToken;
            }
            
            // Helpful logging for debugging 401 vs 404
             var err = await response.Content.ReadAsStringAsync();
             _logger.LogWarning($"Login failed: {response.StatusCode} - {err}");
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed");
            return null;
        }
    }

    private async Task<bool> RegisterUserAsync(string username, string email, string password)
    {
        try
        {
            var response = await _ssoClient.PostAsJsonAsync("/api/v1/Auth/register", new
            {
                 Username = username, 
                 Email = email,
                 Password = password,
                 FirstName = "Test",
                 LastName = "User",
                 TermsAccepted = true
            });

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Registration successful.");
                return true;
            }
            
            var err = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"Registration failed: {response.StatusCode} - {err}");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration exception");
            return false;
        }
    }

    private async Task<string?> RegisterDeviceAsync(string userToken, string deviceId, string deviceName, string deviceType, Guid? tenantId)
    {
        try
        {
            _ssoClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);
            
            // Devices/register endpoint
            // SsoController usually has DevicesController -> Register
            // Route: /api/v1/Devices/register ?
            // I need to be sure. Assumed from previous code.
            
            var response = await _ssoClient.PostAsJsonAsync("/api/v1/devices/register", new
            {
                DeviceId = deviceId,
                DeviceName = deviceName,
                DeviceType = deviceType,
                TenantId = tenantId
            });

            if (!response.IsSuccessStatusCode)
            {
               // If already exists, we might get 400.
               // But how to get token if exists? 
               // For devices, we usually negotiate a new token via Login with Device credentials or Refresh Token flow?
               // Wait, DevicesController Register returns the Device Token (AccessToken).
               // If it already exists, usually we just update it or return error?
               // Previous code handled "already registered" by ignoring error? But then it returned null?
               // No, previous code logged info "Device already registered" and returned NULL!
               // But then how did we seed history? We need a token!
               // If device handles its own auth, it usually does Login.
               // But for seeding, we need a token.
               // If Register fails, we should try to Login as Device? 
               // Or maybe strict generator always registers new devices (using GUIDs)? 
               // For "EnsureTestUserEnvironment", we reuse "desktop_test_01".
               // If it exists, we can't get clarity on secret?
               // Actually, `Device` entity has no password. It uses `DeviceId` trust anchor? 
               // TGP auth: Device sends `Device-Id` header? Or uses Client Credentials?
               // In `Login` DTO, there is `IsDeviceAuth`.
               // Let's assume for now we Register. If it fails, we assume we can't get a token easily unless we re-register or use a new ID.
               
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning($"Device registration failed: {response.StatusCode} - {error}");
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<DeviceRegistrationResponse>();
            return result?.AccessToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Device registration exception");
            return null;
        }
    }

    private record LoginResponse(string AccessToken);
    private record DeviceRegistrationResponse(string AccessToken);
}