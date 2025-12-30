# Entity Relationship Diagram (ERD)

## Core Entities Overview

```mermaid
erDiagram
    Tenant ||--o{ TenantMember : has
    Tenant ||--o{ Device : owns
    Tenant ||--o{ Subscription : has
    
    ApplicationUser ||--o{ TenantMember : "belongs to"
    ApplicationUser ||--o{ RefreshToken : has
    ApplicationUser ||--o{ MfaConfiguration : has
    ApplicationUser ||--o{ AuditLog : generates
    
    Device ||--o{ MonitoredUser : monitors
    Device ||--o{ DeviceCommand : receives
    Device ||--o{ DeviceHeartbeat : sends
    Device ||--o{ MonitoringBatch : uploads
    Device ||--o{ DetectedUser : detects
    Device ||--|| DeviceConfiguration : has
    
    MonitoredUser ||--|| MonitoredUserSettings : has
    MonitoredUser ||--o{ Alert : triggers
    MonitoredUser ||--o{ AppUsageMetric : generates
    
    Plan ||--o{ Subscription : offers
    
    ApplicationRole ||--o{ ApplicationUserRole : has
    ApplicationUser ||--o{ ApplicationUserRole : has
    ApplicationRole ||--o{ RolePermission : has
    Permission ||--o{ RolePermission : "granted via"
```

## Entity Groups

### Identity & Access
- **ApplicationUser**: User accounts
- **ApplicationRole**: Roles (Admin, Parent, etc.)
- **Permission**: Granular permissions
- **RefreshToken**: JWT refresh tokens
- **MfaConfiguration**: Multi-factor auth settings

### Tenancy
- **Tenant**: Organization/family unit
- **TenantMember**: User-Tenant membership
- **Subscription**: Billing subscription
- **Plan**: Subscription tiers

### Devices
- **Device**: Enrolled devices (Windows, Android, ChromeOS)
- **DeviceConfiguration**: Per-device settings
- **DeviceHeartbeat**: Health check records
- **DeviceCommand**: Remote commands

### Monitoring
- **MonitoredUser**: Users being monitored on devices
- **MonitoredUserSettings**: Monitoring rules per user
- **MonitoringBatch**: Uploaded telemetry batches
- **DetectedUser**: New users pending review
- **AppUsageMetric**: Application usage data
- **Alert**: Triggered alerts

### Security & Audit
- **AuditLog**: Security audit trail
- **LoginAttempt**: Login attempt tracking
- **ParentalConsent**: COPPA consent records
