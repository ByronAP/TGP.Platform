# Design Document: Dashboard Data Binding Audit

## Overview

This design document outlines the technical approach for auditing and ensuring all pages and controls in the TGP User Dashboard are properly connected to real live data sources. The audit covers infrastructure connectivity, authentication flows, real-time communication via SignalR, and data binding for all Razor Pages.

## Architecture

The User Dashboard follows a layered architecture. Key architectural decisions:

- **Database migrations** are handled by `TGP.DbMigrator` service (not the dashboard) to avoid race conditions in scaled deployments
- **Device Gateway** is exclusively for device-to-server communication; the dashboard reads device data from the shared database
- **S3 storage** is read-only for the dashboard (viewing screenshots/logs uploaded by device agents)

```
┌─────────────────────────────────────────────────────────────────┐
│                        Razor Pages (UI)                          │
│  Index, Dashboard, Devices, Children, Timeline, Account, etc.   │
├─────────────────────────────────────────────────────────────────┤
│                      Page Models (Controllers)                   │
│  Handle HTTP requests, bind data, invoke services               │
├─────────────────────────────────────────────────────────────────┤
│                         Services Layer                           │
│  DeviceManagementService, AlertsService, TimelineService,       │
│  ChildrenService, TenantService, UserPreferencesService         │
├─────────────────────────────────────────────────────────────────┤
│                      External Clients                            │
│  TgpApiClient (SSO, Reporting), StorageService (S3 read-only)   │
├─────────────────────────────────────────────────────────────────┤
│                      Data Access Layer                           │
│  TgpDbContext (Entity Framework Core + PostgreSQL)              │
├─────────────────────────────────────────────────────────────────┤
│                      Infrastructure                              │
│  PostgreSQL │ Redis │ RabbitMQ │ S3/MinIO │ SignalR             │
└─────────────────────────────────────────────────────────────────┘
```

### Data Flow Architecture

```
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│  Windows Client  │────▶│  Device Gateway  │────▶│   PostgreSQL     │
│  (Device Agent)  │     │  (Device API)    │     │   (Shared DB)    │
└──────────────────┘     └──────────────────┘     └──────────────────┘
                                                          │
                                                          ▼
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│     Browser      │◀────│  User Dashboard  │◀────│   Read Data      │
│     (Parent)     │     │  (This App)      │     │   from DB        │
└──────────────────┘     └──────────────────┘     └──────────────────┘
```

### Real-Time Architecture (SignalR)

The dashboard hosts its own SignalR hub for pushing real-time updates to connected browsers:

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Browser    │────▶│  TimelineHub │────▶│ Redis        │
│   Client     │◀────│  (SignalR)   │◀────│ Backplane    │
└──────────────┘     └──────────────┘     └──────────────┘
                            │
                            ▼
                     ┌──────────────┐
                     │  RabbitMQ    │
                     │  Consumer    │
                     └──────────────┘
```

## Components and Interfaces

### Infrastructure Services

| Component | Interface | Implementation | Purpose |
|-----------|-----------|----------------|---------|
| Database | TgpDbContext | Entity Framework Core | PostgreSQL data access (read/write) |
| Cache | IDistributedCache | Redis | Session and data caching |
| Cache Service | ICacheService | CacheService | Cache-aside pattern for DB queries |
| Messaging | IMessagePublisher | RabbitMqPublisher | Async command/config publishing |
| Storage | IStorageService | StorageService | S3 pre-signed URLs (read-only) |
| API Client | ITgpApiClient | TgpApiClient | SSO and Reporting APIs |

### ITgpApiClient Interface Extensions

The following methods must be added to `ITgpApiClient` for account deletion support:

```csharp
// Account Deletion
Task<AccountDeletionResult> DeleteAccountAsync(string password);
Task<TenantStatusResult> GetTenantStatusAsync();
```

### Page Services

| Service | Responsibility | Data Source |
|---------|---------------|-------------|
| DeviceManagementService | Device CRUD, commands, config | TgpDbContext + RabbitMQ |
| AlertsService | Alert queries | TgpDbContext |
| TimelineService | Aggregated timeline events | TgpDbContext + StorageService |
| ChildrenService | Profile management | TgpDbContext (raw SQL) |
| TenantService | Tenant membership | TgpDbContext |
| UserPreferencesService | User settings | TgpDbContext (raw SQL) |
| CapabilityService | Platform capabilities | Static configuration |

### SignalR Hubs

| Hub | Purpose | Methods |
|-----|---------|---------|
| TimelineHub | Real-time updates | ReceiveAlert, ReceiveDeviceStatus, ReceiveCommand, ReceiveTimelineEvent |

## Data Models

### Core Entities (TGP.Data)

The following entities are stored in the shared TGP.Data project and map to PostgreSQL tables:

```csharp
// Device Management
Device { Id, DeviceId, DeviceName, DeviceType, UserId, TenantId, IsActive, LastSeen }
DeviceConfiguration { Id, DeviceId, Version, TimezoneOffsetMinutes, PollingIntervalSeconds, ... }
DeviceCommand { Id, DeviceId, CommandType, Parameters, Status, IssuedAt, AcknowledgedAt, CompletedAt }
DeviceHeartbeat { Id, DeviceId, ReceivedAt, ClientVersion, UptimeSeconds, ConfigVersion }

// Monitoring
MonitoredUser { Id, DeviceId, Username, IsEnabled, CreatedAt }
MonitoredUserSettings { Id, MonitoredUserId, SettingsJson }
DetectedUser { Id, DeviceId, Username, DetectedAt, IsReviewed, ReviewedBy, ReviewedAt }
Alert { Id, DeviceId, Keyword, Source, DetectedAtUtc }
MonitoringBatch { Id, DeviceId, ReceivedAt, S3Path, CompressedSize, CompressionAlgorithm }

// User Management
ApplicationUser { Id, UserName, Email, IsVerified, TermsAccepted, ... }
Tenant { Id, Name, OwnerId, IsVerified, 
         // New fields for soft delete support (to be added)
         Status,              // "Active" or "SoftDeleted"
         SoftDeletedAt,       // DateTime? - when deletion was initiated
         DeletionEffectiveDate // DateTime? - when permanent deletion occurs (SoftDeletedAt + 7 days)
       }
TenantMember { Id, TenantId, UserId, Role, JoinedAt }

// Profiles (Custom Tables)
MonitoredProfiles { Id, OwnerUserId, DisplayName, Color, CreatedAt, UpdatedAt }
MonitoredProfileLinks { Id, ProfileId, DeviceId, Username, CreatedAt }
```

### API DTOs (TGP.UserDashboard/Services/TgpApiClient.cs)

These DTOs are used for API communication between the UserDashboard and backend services. They are defined in the UserDashboard project, not TGP.Data:

```csharp
// API Responses
DeviceDto { Id, Name, Type, IsOnline, LastSeen }
AlertDto { Id, Message, Timestamp, Severity }
TimelineEventDto { Timestamp, Type, DeviceId, DeviceName, Title, Detail, Link }
ChildProfileDto { Id, Name, Color, LinkedAccounts }
SubscriptionDto { PlanName, Status, NextBillingDate, Amount, Interval }

// Login Response (extended for tenant status)
LoginResult { 
    Success, RequiresMfa, AccessToken, RefreshToken, ExpiresIn, 
    SessionToken, UserId, Message, Error,
    // New fields for soft delete support
    TenantStatus,           // "Active" or "SoftDeleted"
    TenantSoftDeleted,      // bool - convenience flag
    SoftDeletedAt,          // DateTime? - when deletion was initiated
    DeletionEffectiveDate   // DateTime? - when permanent deletion occurs
}

// Account Deletion
AccountDeletionRequestDto { Password }
AccountDeletionResponseDto { Success, DeletionEffectiveDate, ErrorMessage }
TenantStatusDto { Status, SoftDeletedAt, DeletionEffectiveDate }

// Account Deletion Result Classes (for TgpApiClient)
AccountDeletionResult { Success, DeletionEffectiveDate, Error }
TenantStatusResult { Status, SoftDeletedAt, DeletionEffectiveDate, IsSoftDeleted }

// SignalR Messages
AlertNotification { AlertId, DeviceId, DeviceName, Severity, Message, Timestamp }
DeviceStatusUpdate { DeviceId, IsOnline, LastSeen }
CommandStatusUpdate { CommandId, DeviceId, Status, UpdatedAt }
```

### Account Deletion Flow

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Account    │────▶│  SSO API     │────▶│  Database    │
│   Settings   │     │  /delete     │     │  Tenant      │
│   Page       │◀────│  endpoint    │◀────│  Status      │
└──────────────┘     └──────────────┘     └──────────────┘
       │
       ▼ (on success)
┌──────────────┐
│   Logout &   │
│   Redirect   │
└──────────────┘

Login Flow with SoftDeleted Check:
┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Login      │────▶│  SSO API     │────▶│  Check       │
│   Page       │     │  /login      │     │  Tenant      │
│              │◀────│              │◀────│  Status      │
└──────────────┘     └──────────────┘     └──────────────┘
       │
       ▼ (if SoftDeleted)
┌──────────────────────────────────┐
│   Display Deletion Message       │
│   + Contact Support Option       │
└──────────────────────────────────┘
```

### New Pages Required

| Page | Location | Purpose |
|------|----------|---------|
| DeleteAccount | Pages/Account/DeleteAccount.cshtml | Account deletion confirmation with password verification |
| AccountDeleted | Pages/Account/AccountDeleted.cshtml | Soft-deleted account landing page with support contact |

### Login Page Modifications

The Login page (`Pages/Account/Login.cshtml.cs`) must be modified to:

1. Check the `LoginResult` for a new `TenantSoftDeleted` flag
2. If `TenantSoftDeleted` is true, redirect to `AccountDeleted` page instead of dashboard
3. The SSO API login response must include tenant status information

### SSO API Response Extension

The SSO API login endpoint must return tenant status in the response:

```json
{
  "accessToken": "...",
  "refreshToken": "...",
  "expiresIn": 3600,
  "tenantStatus": "Active" | "SoftDeleted",
  "softDeletedAt": "2025-01-01T00:00:00Z",  // only if SoftDeleted
  "deletionEffectiveDate": "2025-01-08T00:00:00Z"  // only if SoftDeleted
}
```

When `tenantStatus` is `SoftDeleted`, the dashboard must:
- NOT set authentication cookies
- Redirect to the AccountDeleted page with deletion info

## Error Handling

### Infrastructure Errors

| Error Type | Handling Strategy | User Feedback |
|------------|-------------------|---------------|
| Database connection failure | Log error, return service unavailable | "Service temporarily unavailable. Please try again." |
| Redis connection failure | Fall back to in-memory, log warning | Silent fallback, no user impact |
| RabbitMQ connection failure | Queue locally, retry with backoff | "Command queued. May take longer to process." |
| S3 connection failure | Display placeholder images, log error | Placeholder image with "Image unavailable" |
| SSO API failure | Display error, allow retry | "Authentication service unavailable. Please try again." |

### Form and Validation Errors

| Error Type | Handling Strategy | User Feedback |
|------------|-------------------|---------------|
| Validation failure | Return to form with errors | Field-level error messages |
| Duplicate entry | Return to form with specific error | "This [item] already exists" |
| Permission denied | Log attempt, display error | "You don't have permission for this action" |
| Not found | Log, redirect or display error | "The requested [item] was not found" |
| Account soft deleted | Reject login, display message | "Your account is pending deletion. Contact support to restore." |

### SignalR Connection Errors

| Error Type | Handling Strategy | User Feedback |
|------------|-------------------|---------------|
| Connection lost | Auto-reconnect with exponential backoff | Connection status indicator |
| Reconnection failed | Continue with manual refresh | "Real-time updates unavailable. Refresh to see latest." |
| Message delivery failure | Log error, no retry | Silent failure, data available on refresh |



## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system-essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

Based on the acceptance criteria analysis, the following properties must hold across all executions:

### Property 1: Tenant Data Isolation

*For any* database query executed by the User_Dashboard and *for any* authenticated user, all returned data records SHALL belong exclusively to the user's tenant.

**Validates: Requirements 1.5, 14.1, 16.1**

This is a critical security property ensuring multi-tenant isolation. No user should ever see data from another tenant.

### Property 2: Deletion Effective Date Calculation

*For any* successful account deletion request, the displayed deletion effective date SHALL be exactly 7 days from the request timestamp.

**Validates: Requirements 26.2**

This ensures consistent communication to users about when their data will be permanently deleted.

### Property 3: SoftDeleted Tenant Login Rejection

*For any* user whose tenant has SoftDeleted status, *for any* login attempt with valid credentials, the User_Dashboard SHALL reject the login and display the pending deletion message with support contact option.

**Validates: Requirements 26.3, 26.4**

This ensures users cannot access accounts that are pending deletion, while providing a path to account recovery.

## Testing Strategy

### Dual Testing Approach

This audit requires both unit testing and property-based testing to ensure comprehensive coverage:

**Unit Tests**: Verify specific examples, edge cases, and integration points
**Property-Based Tests**: Verify universal properties that should hold across all inputs

### Property-Based Testing Framework

**Framework**: xUnit Theory with InlineData and custom data generators
**Approach**: Use `[Theory]` with `[MemberData]` or `[ClassData]` for generating test inputs

### Property-Based Tests

Each property-based test must be tagged with the format: `**Feature: dashboard-data-binding-audit, Property {number}: {property_text}**`

#### Property 1 Test: Tenant Data Isolation

```csharp
// **Feature: dashboard-data-binding-audit, Property 1: Tenant Data Isolation**
// For any query and any authenticated user, results contain only that user's tenant data
public class TenantDataIsolationTests
{
    public static IEnumerable<object[]> TenantIdGenerator()
    {
        // Generate multiple tenant IDs for testing
        for (int i = 0; i < 100; i++)
        {
            yield return new object[] { Guid.NewGuid() };
        }
    }

    [Theory]
    [MemberData(nameof(TenantIdGenerator))]
    public async Task AllQueriesReturnOnlyUserTenantData(Guid tenantId)
    {
        // Setup: Create user context with specific tenant
        // Execute: Run query through service layer
        // Assert: All returned records have matching tenantId
    }
}
```

#### Property 2 Test: Deletion Effective Date Calculation

```csharp
// **Feature: dashboard-data-binding-audit, Property 2: Deletion Effective Date Calculation**
// For any deletion request timestamp, effective date is exactly 7 days later
public class DeletionEffectiveDateTests
{
    public static IEnumerable<object[]> DateTimeGenerator()
    {
        var random = new Random(42); // Seeded for reproducibility
        var baseDate = new DateTime(2020, 1, 1);
        for (int i = 0; i < 100; i++)
        {
            var daysOffset = random.Next(0, 3650); // ~10 years range
            yield return new object[] { baseDate.AddDays(daysOffset).AddSeconds(random.Next(0, 86400)) };
        }
    }

    [Theory]
    [MemberData(nameof(DateTimeGenerator))]
    public void EffectiveDateIsExactlySevenDaysFromRequest(DateTime requestTimestamp)
    {
        var result = AccountDeletionService.CalculateEffectiveDate(requestTimestamp);
        Assert.Equal(requestTimestamp.AddDays(7), result);
    }
}
```

#### Property 3 Test: SoftDeleted Tenant Login Rejection

```csharp
// **Feature: dashboard-data-binding-audit, Property 3: SoftDeleted Tenant Login Rejection**
// For any user with SoftDeleted tenant, login is always rejected
public class SoftDeletedTenantLoginTests
{
    public static IEnumerable<object[]> CredentialsGenerator()
    {
        var random = new Random(42);
        var usernames = new[] { "user@test.com", "admin@example.org", "parent@family.net" };
        for (int i = 0; i < 100; i++)
        {
            var username = usernames[random.Next(usernames.Length)] + random.Next(1000);
            var password = $"ValidPass{random.Next(10000)}!";
            yield return new object[] { username, password };
        }
    }

    [Theory]
    [MemberData(nameof(CredentialsGenerator))]
    public async Task LoginAlwaysRejectedForSoftDeletedTenant(string username, string password)
    {
        // Setup: Create user with SoftDeleted tenant status
        // Execute: Attempt login with valid credentials
        // Assert: Login rejected with appropriate message
    }
}
```

### Unit Tests

Unit tests should cover:

1. **Infrastructure Connectivity**
   - Database connection establishment and failure handling
   - Redis connection and fallback behavior
   - RabbitMQ message publishing
   - S3 pre-signed URL generation
   - SSO API authentication flows

2. **Authentication Flows**
   - Login with valid/invalid credentials
   - MFA verification
   - Token refresh
   - Logout and session cleanup
   - Account deletion flow

3. **Page Data Binding**
   - Each page loads correct data for authenticated user
   - Forms submit and validate correctly
   - Error states display appropriately

4. **SignalR Real-Time Updates**
   - Connection establishment
   - Message receipt and UI updates
   - Reconnection behavior

### Integration Tests

Integration tests should verify end-to-end flows:

1. **Account Deletion Flow**
   - User initiates deletion → SSO API called → Confirmation displayed
   - User with SoftDeleted tenant attempts login → Rejected with message

2. **Device Command Flow**
   - Command created → Published to RabbitMQ → Status updates via SignalR

3. **Real-Time Alert Flow**
   - Alert detected → Pushed via SignalR → UI notification displayed
