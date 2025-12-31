using System.Text.Json.Serialization;

namespace TGP.TestDataGenerator.Models;

public class EventBatch
{
    public string BatchId { get; set; } = Guid.NewGuid().ToString("N");
    public List<TestEventArgs> Events { get; set; } = new();
    // Simplified for generator - omitting screenshots for now
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int SessionId { get; set; } = 1;
    public string CompressionAlgorithm { get; set; } = "none";
    public byte[]? CompressedData { get; set; } // Will be null before sending
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ProcessEventArgs), "TGP.Windows.Monitoring.Events.ProcessEventArgs")]
[JsonDerivedType(typeof(UrlChangedEventArgs), "TGP.Windows.Monitoring.Events.UrlChangedEventArgs")]
[JsonDerivedType(typeof(ScreenshotEventArgs), "TGP.Windows.Monitoring.Events.ScreenshotEventArgs")]
public class TestEventArgs
{
    // Base marker class
}

public class ProcessEventArgs : TestEventArgs
{
    public required ProcessInfo Process { get; init; }
    public required DateTime Timestamp { get; init; }
}

public class UrlChangedEventArgs : TestEventArgs
{
    public required BrowserInfo Browser { get; init; }
    public string? PreviousUrl { get; init; }
    public required string NewUrl { get; init; }
    public required DateTime Timestamp { get; init; }
}

public class ScreenshotEventArgs : TestEventArgs
{
    public required DateTime Timestamp { get; init; }
    public required string FileName { get; init; }
    public required string Base64Data { get; init; }
    public string? WindowTitle { get; init; }
    public string? ProcessName { get; init; }
}

public class ProcessInfo
{
    public required int ProcessId { get; init; }
    public required string ProcessName { get; init; }
    public string? ExecutablePath { get; init; }
    public string? CommandLine { get; init; }
    public int SessionId { get; init; }
    public DateTime? StartTime { get; init; }
    public string? MainWindowTitle { get; init; }
}

public class BrowserInfo
{
    public required int ProcessId { get; init; }
    public required string ProcessName { get; init; }
    public required string BrowserType { get; init; } = "Chrome"; // Simplified enum
    public required string BrowserName { get; init; } = "Google Chrome";
    public string? CurrentUrl { get; init; }
    public string? WindowTitle { get; init; }
    public int SessionId { get; init; }
}
