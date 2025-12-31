using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using TGP.TestDataGenerator.Models;

namespace TGP.TestDataGenerator.Services;

/// <summary>
/// Seeds historical batch data via the Gateway API.
/// </summary>
public class BatchSeeder
{
    private readonly HttpClient _gatewayClient;
    private readonly ILogger<BatchSeeder> _logger;
    private readonly Random _rng = new();

    public BatchSeeder(IConfiguration config, ILogger<BatchSeeder> logger)
    {
        _logger = logger;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        var gatewayUrl = config["Gateway:Url"] ?? "https://localhost:8080";
        _gatewayClient = new HttpClient(handler) { BaseAddress = new Uri(gatewayUrl) };
    }

    /// <summary>
    /// Seeds historical batch data by sending batches to the Gateway API.
    /// </summary>
    public async Task SeedHistoryAsync(string deviceToken, string deviceId, bool includeScreenshots = false)
    {
        _logger.LogInformation("Sending historic batches via API...");
        
        // Extract correct DeviceID from Token (as per server requirement)
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(deviceToken);
        var deviceIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "device_id");
        
        // Use claim if available (Server DB ID), otherwise fallback to local ID (Client String ID)
        var actualDeviceId = deviceIdClaim?.Value ?? deviceId;

        // Update LastSeen and Report User IMMEDIATELY
        await SendHeartbeatAsync(deviceToken, actualDeviceId);
        await ReportDetectedUserAsync(deviceToken, actualDeviceId, "test_user");

        var now = DateTime.UtcNow;
        var startTime = now.AddHours(-24);
        
        int totalMinutes = 24 * 60;
        int stepMinutes = 5;

        for (int m = 0; m < totalMinutes; m += stepMinutes)
        {
            var timestamp = startTime.AddMinutes(m);
            await SendBatchAsync(deviceToken, actualDeviceId, timestamp, includeScreenshots);
            await Task.Delay(50); 
        }

        _logger.LogInformation("History seeding complete.");
        
        // Final heartbeat
        await SendHeartbeatAsync(deviceToken, actualDeviceId);
    }

    private async Task SendBatchAsync(string token, string deviceId, DateTime timestamp, bool includeScreenshot)
    {
        var batchId = Guid.NewGuid().ToString();
        var batch = new EventBatch { SessionId = 1, BatchId = batchId };
        
        // Add random events appropriate for the time of day
        int hour = timestamp.Hour;
        bool isNight = hour >= 0 && hour < 6;
        
        if (isNight && _rng.NextDouble() > 0.2) 
        {
            // Mostly idle at night
        }
        else
        {
            // Add App Usage
            int appCount = _rng.Next(5, 15);
            for(int j=0; j<appCount; j++)
            {
                batch.Events.Add(new ProcessEventArgs
                {
                    Timestamp = timestamp.AddMinutes(_rng.Next(0, 60)),
                    Process = new ProcessInfo
                    {
                        ProcessId = _rng.Next(1000, 9999),
                        ProcessName = GetWeightedApp(),
                        MainWindowTitle = "Active Window",
                        SessionId = 1
                    }
                });
            }

            // Add Screenshot if requested and on Windows
            if (includeScreenshot && OperatingSystem.IsWindows())
            {
                batch.Events.Add(GenerateTestScreenshot(1, timestamp));
            }
        }

        var json = JsonSerializer.Serialize(batch);
        var bytes = Encoding.UTF8.GetBytes(json);

        using var content = new ByteArrayContent(bytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Headers.Add("X-Batch-Id", batchId);
        content.Headers.Add("X-Compression", "none");
        content.Headers.Add("X-Session-Id", "1");
        content.Headers.Add("X-Created-At", timestamp.ToString("O"));

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/windows/batches");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("X-Device-Id", deviceId);
        req.Content = content;

        try 
        {
            var res = await _gatewayClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Uploaded batch for {timestamp:t}");
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                
                if (res.StatusCode == System.Net.HttpStatusCode.InternalServerError)
                {
                     _logger.LogWarning($"Server rejected batch (500). Likely cause: User '{deviceId}' has no Tenant (Production Requirement). Skipping batch.");
                }
                else
                {
                    _logger.LogError($"Failed batch upload: {res.StatusCode} - {err}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send batch request");
        }
    }

    private async Task SendHeartbeatAsync(string token, string deviceId)
    {
        var hb = new HeartbeatDto(
            ClientVersion: "2.1.0",
            UptimeSeconds: 86400,
            QueueDepth: 0,
            LastSyncUtc: DateTimeOffset.UtcNow,
            ActiveSessionCount: 1,
            ActiveUsers: new List<string> { "test_user" },
            ConnectionMode: "http",
            ConfigVersion: 1,
            HardwareTier: "high",
            NetworkSpeed: "fast"
        );

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/windows/heartbeat");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("X-Device-Id", deviceId);
        req.Content = JsonContent.Create(hb);

        try 
        {
            var res = await _gatewayClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                _logger.LogInformation("Sent heartbeat to update LastSeen.");
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                _logger.LogError($"Failed heartbeat: {res.StatusCode} - {err}");
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Failed to send heartbeat");
        }
    }

    private string GetWeightedApp()
    {
        double r = _rng.NextDouble();
        if (r < 0.4) return "chrome";
        if (r < 0.6) return "Code"; // VS Code
        if (r < 0.8) return "Discord";
        if (r < 0.9) return "Minecraft";
        return "Notepad";
    }

    /// <summary>
    /// Reports a detected Windows user to trigger dashboard alerts.
    /// </summary>
    private async Task ReportDetectedUserAsync(string token, string deviceId, string username)
    {
        var request = new { Username = username, SessionId = 1 };
        
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/windows/users/detected");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Headers.Add("X-Device-Id", deviceId);
        req.Content = JsonContent.Create(request);

        try
        {
            var res = await _gatewayClient.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Reported detected user '{username}' - dashboard should now show alert.");
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                _logger.LogWarning($"Failed to report detected user: {res.StatusCode} - {err}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to report detected user");
        }
    }

    [SupportedOSPlatform("windows")]
    private TestEventArgs GenerateTestScreenshot(int sessionId, DateTime timestamp)
    {
        int width = 800;
        int height = 600;

        using var bitmap = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.LightGray);
            g.FillRectangle(Brushes.White, 50, 50, 700, 500);
            g.DrawRectangle(Pens.Blue, 50, 50, 700, 500);
            
            using var font = new Font(FontFamily.GenericSansSerif, 16, FontStyle.Bold);
            g.DrawString($"TEST CAPTURE", font, Brushes.Black, 60, 60);
            g.DrawString($"Time: {timestamp}", font, Brushes.DarkGray, 60, 90);
            g.DrawString($"App: {GetWeightedApp()}", font, Brushes.Blue, 60, 120);
            g.FillEllipse(Brushes.Red, _rng.Next(100, 700), _rng.Next(100, 500), 50, 50);
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        var base64 = Convert.ToBase64String(ms.ToArray());

        return new ScreenshotEventArgs
        {
            Timestamp = timestamp,
            FileName = $"test_screen_{timestamp.Ticks}.png",
            Base64Data = base64,
            WindowTitle = $"Test Session {sessionId}",
            ProcessName = "synthetictest"
        };
    }
}
