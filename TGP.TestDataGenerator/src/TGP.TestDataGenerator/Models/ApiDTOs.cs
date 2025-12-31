namespace TGP.TestDataGenerator.Models;

/// <summary>
/// Heartbeat DTO matching DeviceGateway's expected schema.
/// </summary>
public record HeartbeatDto(
    string ClientVersion,
    long UptimeSeconds,
    int QueueDepth,
    DateTimeOffset LastSyncUtc,
    int ActiveSessionCount,
    List<string>? ActiveUsers,
    string ConnectionMode,
    int ConfigVersion,
    string HardwareTier,
    string NetworkSpeed
);

// Auth DTOs
public record LoginResponse(string AccessToken, string RefreshToken, int ExpiresIn);
public record DeviceRegistrationResponse(string AccessToken, string RefreshToken);
