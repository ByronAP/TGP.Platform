using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.SignalR.Client;
using TGP.TestDataGenerator.Models;

namespace TGP.TestDataGenerator.Services;

public class TrafficSimulator
{
    private readonly HttpClient _gatewayHttp;
    private readonly List<VirtualAgent> _agents = new();
    private readonly string _baseUrl;

    public TrafficSimulator(string gatewayUrl)
    {
        _baseUrl = gatewayUrl;
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _gatewayHttp = new HttpClient(handler) { BaseAddress = new Uri(gatewayUrl) };
    }

    public void LoadAgents(List<SeededAgent> seededAgents)
    {
        Console.WriteLine($"Loading {seededAgents.Count} agents...");
        foreach (var sa in seededAgents)
        {
            var agent = new VirtualAgent(sa.DeviceId, sa.Token, sa.Type, _gatewayHttp, _baseUrl);
            _agents.Add(agent);
        }
        Console.WriteLine("Agents loaded.");
    }

    public async Task RunAsync(CancellationToken ct)
    {
        Console.WriteLine($"\nStarting simulation for {_agents.Count} agents. Press Ctrl+C to stop.");

        var tasks = _agents.Select(a => a.RunLoopAsync(ct));
        await Task.WhenAll(tasks);
    }
}

public class VirtualAgent
{
    private readonly string _deviceId;
    private readonly string _token;
    private readonly string _type;
    private readonly HttpClient _client;
    private readonly HubConnection _hubConnection;
    private readonly Random _rng = new();

    public VirtualAgent(string deviceId, string token, string type, HttpClient client, string baseUrl)
    {
        _token = token;
        
        // Extract correct DeviceID from Token (as per server requirement)
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(_token);
        var deviceIdClaim = jwt.Claims.FirstOrDefault(c => c.Type == "device_id");
        
        // Use claim if available (Server DB ID), otherwise fallback to local ID (Client String ID)
        _deviceId = deviceIdClaim?.Value ?? deviceId;
        _type = type;
        _client = client;

        var encodedToken = Uri.EscapeDataString(_token);
        var hubUrl = $"{baseUrl}/hubs/v1/devices/device?access_token={encodedToken}";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.Headers.Add("X-Device-Id", _deviceId);
                options.HttpMessageHandlerFactory = (handler) =>
                {
                    if (handler is HttpClientHandler clientHandler)
                    {
                        clientHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                    }
                    return handler;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.Closed += async (error) =>
        {
            Message("SignalR connection closed. Restarting...");
            await Task.Delay(new Random().Next(0,5) * 1000);
            try { await _hubConnection.StartAsync(); } catch {}
        };

        // Command Handling
        _hubConnection.On<CommandDto>("CommandReceived", async (cmd) =>
        {
            Message($"Received Command: {cmd.Type} (ID: {cmd.Id})");
            await HandleCommand(cmd);
        });
    }

    private async Task HandleCommand(CommandDto cmd)
    {
        await AcknowledgeCommand(cmd.Id);
        await Task.Delay(2000); 
        await CompleteCommand(cmd.Id, "success");
    }

    private async Task AcknowledgeCommand(Guid id)
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/windows/commands/{id}/ack");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            req.Headers.Add("X-Device-Id", _deviceId);
            var res = await _client.SendAsync(req);
            if (!res.IsSuccessStatusCode) Message($"Ack Failed: {res.StatusCode}");
        }
        catch (Exception ex) { Message($"Ack Error: {ex.Message}"); }
    }

    private async Task CompleteCommand(Guid id, string status)
    {
         try
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/windows/commands/{id}/result");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            req.Headers.Add("X-Device-Id", _deviceId);
            req.Content = JsonContent.Create(new { Status = status });
            var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode) Message($"Command {id} Completed");
        }
        catch (Exception ex) { Message($"Result Error: {ex.Message}"); }
    }

    public async Task RunLoopAsync(CancellationToken ct)
    {
        try
        {
            await _hubConnection.StartAsync(ct);
            Message("SignalR Connected");
        }
        catch (Exception ex)
        {
            Message($"SignalR Connection Failed: {ex.Message}");
        }

        int tick = 0;
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Every 30s: Heartbeat
                if (tick % 30 == 0)
                {
                    HeartbeatResponse? hbResponse = null;

                    if (_hubConnection.State == HubConnectionState.Connected)
                    {
                        // SignalR Heartbeat returns HeartbeatResponse
                        hbResponse = await _hubConnection.InvokeAsync<HeartbeatResponse>("Heartbeat", CreateHeartbeatDto(), ct);
                        Message("Heartbeat (SignalR) Sent");
                    }
                    else
                    {
                        // Fallback HTTP Heartbeat
                        hbResponse = await SendHeartbeatHttp();
                    }

                    // Process Response
                    if (hbResponse != null)
                    {
                        if (hbResponse.HasConfigUpdate)
                        {
                            Message("Config Update Flagged. Syncing...");
                            await SyncConfiguration();
                        }
                        if (hbResponse.HasPendingCommands)
                        {
                            Message("Pending Commands Flagged (Simulated Pull)");
                            // Real client might HTTP GET /commands, but SignalR usually pushes them.
                            // We do nothing here as SignalR push handles it, or assuming GET logic if needed.
                        }
                    }
                }

                // Every 60s: Batch
                if (tick % 60 == 0 && _type == "Windows") 
                {
                    await SendEventBatch();
                }

                await Task.Delay(1000, ct);
                tick++;
            }
            catch (TaskCanceledException) { break; }
            catch (Exception ex)
            {
                // Loop error
            }
        }
    }

    private HeartbeatDto CreateHeartbeatDto()
    {
        return new HeartbeatDto(
            ClientVersion: "1.0.0",
            UptimeSeconds: 3600,
            QueueDepth: 0,
            LastSyncUtc: DateTimeOffset.UtcNow,
            ActiveSessionCount: 1,
            ActiveUsers: new List<string> { "test_user" },
            ConnectionMode: "signalr",
            ConfigVersion: 1,
            HardwareTier: "high",
            NetworkSpeed: "fast"
        );
    }

    private async Task<HeartbeatResponse?> SendHeartbeatHttp()
    {
        try
        {
            var hb = CreateHeartbeatDto();
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/windows/heartbeat");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            req.Headers.Add("X-Device-Id", _deviceId);
            req.Content = JsonContent.Create(hb);

            var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                Message($"Heartbeat (HTTP) OK");
                return await res.Content.ReadFromJsonAsync<HeartbeatResponse>();
            }
            else
            {
                Message($"Heartbeat (HTTP) Fail: {res.StatusCode}");
                return null;
            }
        }
        catch { return null; }
    }

    private async Task SyncConfiguration()
    {
        try
        {
            var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/windows/config");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            req.Headers.Add("X-Device-Id", _deviceId);
            
            var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode)
            {
                Message("Config Synced Successfully");
            }
            else
            {
                Message($"Config Sync Failed: {res.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Message($"Config Sync Error: {ex.Message}");
        }
    }

    private async Task SendEventBatch()
    {
        try {
            var batch = new EventBatch { SessionId = 1 };
            int eventCount = _rng.Next(1, 5); 

            for (int i = 0; i < eventCount; i++)
            {
                double r = _rng.NextDouble();
                if (r > 0.9) batch.Events.Add(GenerateScreenshot(1));
                else if (r > 0.5)
                {
                    batch.Events.Add(new ProcessEventArgs
                    {
                        Timestamp = DateTime.UtcNow,
                        Process = new ProcessInfo
                        {
                            ProcessId = _rng.Next(1000, 9999),
                            ProcessName = GetRandomApp(),
                            MainWindowTitle = "App Window",
                            SessionId = 1
                        }
                    });
                }
                else
                {
                    var (url, title) = GetRandomSite();
                    batch.Events.Add(new UrlChangedEventArgs
                    {
                        Timestamp = DateTime.UtcNow,
                        NewUrl = url,
                        PreviousUrl = "about:blank",
                        Browser = new BrowserInfo
                        {
                            ProcessId = _rng.Next(1000, 9999),
                            ProcessName = "chrome",
                            BrowserName = "Chrome",
                            BrowserType = "Chrome",
                            CurrentUrl = url,
                            WindowTitle = title,
                            SessionId = 1
                        }
                    });
                }
            }

            var json = JsonSerializer.Serialize(batch);
            var bytes = Encoding.UTF8.GetBytes(json);

            using var content = new ByteArrayContent(bytes);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            content.Headers.Add("X-Batch-Id", batch.BatchId);
            content.Headers.Add("X-Compression", "none");
            content.Headers.Add("X-Session-Id", "1");
            content.Headers.Add("X-Created-At", DateTime.UtcNow.ToString("O"));

            var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/windows/batches");
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            req.Headers.Add("X-Device-Id", _deviceId);
            req.Content = content;

            var res = await _client.SendAsync(req);
            if (res.IsSuccessStatusCode) Message($"Batch Upload OK ({eventCount} events)");
            else Message($"Batch Upload Fail: {res.StatusCode}");
        }
        catch(Exception ex) { Message($"Batch Error: {ex.Message}"); }
    }

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);
    const int SM_CXSCREEN = 0;
    const int SM_CYSCREEN = 1;

    private TestEventArgs GenerateScreenshot(int sessionId)
    {
        if (!OperatingSystem.IsWindows()) return GeneratePlaceholderScreenshot(sessionId);
        try { return GenerateWindowsScreenshot(sessionId); }
        catch { return GeneratePlaceholderScreenshot(sessionId); }
    }

    private static TestEventArgs GeneratePlaceholderScreenshot(int sessionId)
    {
        const string oneByOneTransparentPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+XxwoAAAAASUVORK5CYII=";
        return new ScreenshotEventArgs
        {
            Timestamp = DateTime.UtcNow,
            FileName = $"placeholder_{DateTime.UtcNow.Ticks}.png",
            Base64Data = oneByOneTransparentPngBase64,
            WindowTitle = $"Session {sessionId}",
            ProcessName = "unknown"
        };
    }

    [SupportedOSPlatform("windows")]
    private TestEventArgs GenerateWindowsScreenshot(int sessionId)
    {
        var width = GetSystemMetrics(SM_CXSCREEN);
        var height = GetSystemMetrics(SM_CYSCREEN);
        if (width <= 0) width = 1920;
        if (height <= 0) height = 1080;

        using var bitmap = new Bitmap(width, height);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.CopyFromScreen(0, 0, 0, 0, new Size(width, height));
            using var font = new Font(FontFamily.GenericSansSerif, 24, FontStyle.Bold);
            g.DrawString($"LIVE CAPTURE: {DateTime.UtcNow:O}", font, Brushes.Red, 50, 50);
        }

        using var ms = new MemoryStream();
        bitmap.Save(ms, ImageFormat.Png);
        var base64 = Convert.ToBase64String(ms.ToArray());
        return new ScreenshotEventArgs
        {
            Timestamp = DateTime.UtcNow,
            FileName = $"screen_{DateTime.UtcNow.Ticks}.png",
            Base64Data = base64,
            WindowTitle = $"Session {sessionId}",
            ProcessName = "explorer"
        };
    }

    private string GetRandomApp() => new[] { "notepad", "chrome", "steam", "discord", "word", "excel" }[_rng.Next(6)];
    private (string, string) GetRandomSite()
    {
        var sites = new[] { ("https://google.com", "Google"), ("https://youtube.com", "YouTube"), ("https://reddit.com", "Reddit") };
        return sites[_rng.Next(3)];
    }

    private void Message(string msg) => Console.WriteLine($"[{_type[0]}:{_deviceId.Substring(0, 8)}] {msg}");
}

public class CommandDto 
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string? Payload { get; set; }
}

public class HeartbeatResponse
{
    public bool HasPendingCommands { get; set; }
    public bool HasConfigUpdate { get; set; }
}
