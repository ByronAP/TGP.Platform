# UI Implementation Guide: Phases 3-9
## Hybrid Parental Control UI Redesign - Continuation

> **📍 YOU ARE HERE**: Detailed Implementation Guide (Phases 3-9)
> 
> **Navigation:**
> - **Main Plan**: See [implementation_plan.md](implementation_plan.md) - Overall design & architecture
> - **Current Status**: See [walkthrough.md](walkthrough.md) - What's complete (Phases 1-2)
> - **Progress Tracking**: See [task.md](task.md) - Mark off items as you complete them
> - **Reference Docs**: [terminology_glossary.md](terminology_glossary.md) | [ux_writing_reference.md](ux_writing_reference.md)
> 
> **Prerequisites**: Phases 1 & 2 complete (data model + backend services deployed) ✅

---

## Table of Contents

1. [Phase 5: UserSettings Page (Complete Implementation)](#phase-5)
2. [Phase 3: Shared UI Components](#phase-3)
3. [Phase 4: Device Flow Pages](#phase-4)
4. [Phase 6: UnassignedUsers Page](#phase-6)
5. [Phase 7: Children Pages](#phase-7)
6. [Phase 8: Terminology Cleanup](#phase-8)
7. [Phase 9: Final Polish](#phase-9)
8. [Testing & Verification](#testing)
9. [Quality Checklist](#quality-checklist)

---

## Phase 5: UserSettings Page (Complete Implementation) {#phase-5}

**Priority**: CRITICAL - This makes the ProfileId feature functional
**File**: `Pages/Devices/UserSettings.cshtml.cs`
**Complexity**: HIGH

### Current State
- ProfileId parameter added (BindProperty)
- ChildName property added
- OnGetAsync and OnPostAsync DO NOT properly handle ProfileId

### Required Changes

#### 1. Update OnGetAsync Method

**Location**: Line ~69-153

**Requirements**:
- Parse ProfileId parameter if provided
- Load child profile information when ProfileId is set
- Query settings based on `(MonitoredUserId, ProfileId)` combination
- Create default settings if none exist
- Display different UI for assigned vs unassigned

**Implementation**:

```csharp
public async Task<IActionResult> OnGetAsync()
{
    if (string.IsNullOrEmpty(DeviceId) || string.IsNullOrEmpty(Username))
    {
        return NotFound();
    }

    if (!Guid.TryParse(DeviceId, out var deviceGuid))
    {
        return NotFound();
    }

    // Get tenant ID for data isolation
    var userId = User.GetUserId();
    if (!userId.HasValue)
    {
        return RedirectToPage("/Account/Login");
    }

    var tenant = await _tenantService.GetOrCreateTenantAsync(userId.Value);
    var tenantId = tenant.Id;

    // Get device with tenant filtering
    var device = await _deviceService.GetDeviceAsync(deviceGuid, tenantId);
    if (device == null)
    {
        _logger.LogWarning("Device {DeviceId} not found for tenant {TenantId}", deviceGuid, tenantId);
        return NotFound();
    }

    DeviceName = device.DeviceName ?? "Unknown Device";
    DeviceType = device.DeviceType ?? "Windows";
    
    // Parse and validate ProfileId if provided
    Guid? profileGuid = null;
    if (!string.IsNullOrEmpty(ProfileId))
    {
        if (!Guid.TryParse(ProfileId, out var parsedProfileId))
        {
            _logger.LogWarning("Invalid ProfileId format: {ProfileId}", ProfileId);
            return BadRequest("Invalid ProfileId");
        }
        
        profileGuid = parsedProfileId;
        
        // Load child profile information
        var profile = await _childrenService.GetProfileAsync(profileGuid.Value, userId.Value, tenantId);
        if (profile == null)
        {
            _logger.LogWarning("Profile {ProfileId} not found for user {UserId}", profileGuid, userId);
            return NotFound("Child profile not found");
        }
        
        ChildName = profile.Name;
    }

    // Get or create MonitoredUser
    var monitoredUser = await _context.MonitoredUsers
        .FirstOrDefaultAsync(mu => mu.DeviceId == deviceGuid && 
            mu.Username.ToLower() == Username.ToLower());

    if (monitoredUser == null)
    {
        // Create new MonitoredUser
        monitoredUser = new MonitoredUser
        {
            DeviceId = deviceGuid,
            Username = Username,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.MonitoredUsers.Add(monitoredUser);
        await _context.SaveChangesAsync();
    }

    MonitoredUserId = monitoredUser.Id;

    // CRITICAL: Get settings based on (MonitoredUserId, ProfileId) combination
    var settings = await _context.MonitoredUserSettings
        .FirstOrDefaultAsync(s => s.MonitoredUserId == monitoredUser.Id && 
            s.ProfileId == profileGuid);

    if (settings == null)
    {
        // Create default settings for this combination
        settings = new MonitoredUserSettings
        {
            MonitoredUserId = monitoredUser.Id,
            ProfileId = profileGuid,
            EnableProcessMonitoring = true,
            EnableBrowserMonitoring = true,
            EnableInputMonitoring = false,
            EnableScreenCapture = false,
            EnableLocationMonitoring = false,
            UpdatedAt = DateTime.UtcNow
        };
        _context.MonitoredUserSettings.Add(settings);
        await _context.SaveChangesAsync();
    }

    // Load available profiles for dropdown
    var profiles = await _childrenService.ListProfilesAsync(userId.Value, tenantId);
    ChildProfiles = profiles.Select(p => new SelectListItem
    {
        Value = p.Id.ToString(),
        Text = p.Name
    }).ToList();

    // Map settings to view model
    Settings = MapToViewModel(monitoredUser, settings);
    
    // Set the current profile ID in the view model if assigned
    if (profileGuid.HasValue)
    {
        Settings.ChildProfileId = profileGuid.Value.ToString();
    }

    return Page();
}
```

#### 2. Update OnPostAsync Method

**Location**: Line ~158-250

**Requirements**:
- Parse ProfileId from form/URL
- Save settings with correct ProfileId
- Validate ProfileId matches the one loaded
- Publish configuration update with correct context

**Implementation**:

```csharp
public async Task<IActionResult> OnPostAsync()
{
    if (string.IsNullOrEmpty(DeviceId) || string.IsNullOrEmpty(Username))
    {
        return NotFound();
    }

    if (!Guid.TryParse(DeviceId, out var deviceGuid))
    {
        return NotFound();
    }

    // Get tenant ID
    var userId = User.GetUserId();
    if (!userId.HasValue)
    {
        return RedirectToPage("/Account/Login");
    }

    var tenant = await _tenantService.GetOrCreateTenantAsync(userId.Value);
    var tenantId = tenant.Id;

    // Verify device belongs to tenant
    var device = await _deviceService.GetDeviceAsync(deviceGuid, tenantId);
    if (device == null)
    {
        _logger.LogWarning("Device {DeviceId} not found for tenant {TenantId} during save", 
            deviceGuid, tenantId);
        return NotFound();
    }

    DeviceName = device.DeviceName ?? "Unknown Device";
    DeviceType = device.DeviceType ?? "Windows";
    
    // Parse ProfileId
    Guid? profileGuid = null;
    if (!string.IsNullOrEmpty(ProfileId))
    {
        if (!Guid.TryParse(ProfileId, out var parsedProfileId))
        {
            ErrorMessage = "Invalid profile ID";
            return Page();
        }
        profileGuid = parsedProfileId;
        
        // Verify profile belongs to user
        var profile = await _childrenService.GetProfileAsync(profileGuid.Value, userId.Value, tenantId);
        if (profile == null)
        {
            ErrorMessage = "Profile not found or access denied";
            return Page();
        }
        ChildName = profile.Name;
    }

    // Find the monitored user
    var monitoredUser = await _context.MonitoredUsers
        .FirstOrDefaultAsync(mu => mu.DeviceId == deviceGuid && 
            mu.Username.ToLower() == Username.ToLower());

    if (monitoredUser == null)
    {
        ErrorMessage = "User not found on this device.";
        return Page();
    }

    // CRITICAL: Find settings for this (MonitoredUserId, ProfileId) combination
    var settings = await _context.MonitoredUserSettings
        .FirstOrDefaultAsync(s => s.MonitoredUserId == monitoredUser.Id && 
            s.ProfileId == profileGuid);

    if (settings == null)
    {
        // Create new settings
        settings = new MonitoredUserSettings
        {
            MonitoredUserId = monitoredUser.Id,
            ProfileId = profileGuid
        };
        _context.MonitoredUserSettings.Add(settings);
    }

    // Update settings from view model
    MapFromViewModel(Settings, settings);
    settings.UpdatedAt = DateTime.UtcNow;

    await _context.SaveChangesAsync();

    // Publish configuration update
    await _deviceService.PublishConfigurationUpdateAsync(deviceGuid);

    SuccessMessage = "Settings saved successfully";
    return RedirectToPage(new { DeviceId, Username, ProfileId });
}
```

#### 3. Update UserSettings.cshtml View

**File**: `Pages/Devices/UserSettings.cshtml`

**Requirements**:
- Display breadcrumb: "Device › Username › Rules for [Child]" or "Device › Username › Basic Rules"
- Show "Connect to child" banner for unassigned logins
- Disable advanced settings for un assigned logins
- Show child selector if needed

**Key Changes**:

```html
@page
@model UserSettingsModel

@{
    ViewData["Title"] = Model.ProfileId != null 
        ? $"Rules for {Model.ChildName}" 
        : "Basic Monitoring Settings";
}

<!-- Breadcrumb -->
<nav aria-label="breadcrumb">
    <ol class="breadcrumb">
        <li class="breadcrumb-item"><a asp-page="/Devices/Index">Devices</a></li>
        <li class="breadcrumb-item"><a asp-page="/Devices/Details" asp-route-id="@Model.DeviceId">@Model.DeviceName</a></li>
        <li class="breadcrumb-item active" aria-current="page">
            @(Model.ProfileId != null ? $"{Model.Username} › {Model.ChildName}" : Model.Username)
        </li>
    </ol>
</nav>

<!-- Page Header -->
<div class="d-flex justify-content-between align-items-center mb-4">
    <div>
        <h2>
            @if (Model.ProfileId != null)
            {
                <text>Rules for @Model.ChildName</text>
            }
            else
            {
                <text>Basic Monitoring Settings</text>
            }
        </h2>
        <p class="text-muted mb-0">
            <strong>Login:</strong> @Model.Username on @Model.DeviceName
        </p>
    </div>
</div>

<!-- Unassigned Banner -->
@if (Model.ProfileId == null)
{
    <div class="alert alert-info" role="alert">
        <h5 class="alert-heading">
            <i class="bi bi-info-circle"></i> Limited Monitoring
        </h5>
        <p>
            This login is not connected to a child profile. Only basic monitoring settings are available.
        </p>
        <p class="mb-0">
            <strong>Want full parental controls?</strong> 
            <a asp-page="/Children/Index" class="alert-link">Connect this login to a child profile</a> 
            to unlock screen time limits, app restrictions, and more.
        </p>
    </div>
}

<!-- Settings Form -->
<form method="post">
    <input type="hidden" asp-for="DeviceId" />
    <input type="hidden" asp-for="Username" />
    <input type="hidden" asp-for="ProfileId" />
    
    <!-- Basic Monitoring (Always Available) -->
    <div class="card mb-4">
        <div class="card-header">
            <h5 class="mb-0">Basic Monitoring</h5>
        </div>
        <div class="card-body">
            <div class="form-check form-switch mb-3">
                <input class="form-check-input" type="checkbox" asp-for="Settings.EnableProcessMonitoring" />
                <label class="form-check-label" asp-for="Settings.EnableProcessMonitoring">
                    Track app usage
                </label>
            </div>
            <div class="form-check form-switch mb-3">
                <input class="form-check-input" type="checkbox" asp-for="Settings.EnableBrowserMonitoring" />
                <label class="form-check-label" asp-for="Settings.EnableBrowserMonitoring">
                    Monitor web browsing
                </label>
            </div>
        </div>
    </div>
    
    <!-- Advanced Settings (Only for assigned logins) -->
    @if (Model.ProfileId != null)
    {
        <div class="card mb-4">
            <div class="card-header">
                <h5 class="mb-0">Advanced Settings</h5>
            </div>
            <div class="card-body">
                <!-- Screen capture, time limits, etc. -->
                <div class="form-check form-switch mb-3">
                    <input class="form-check-input" type="checkbox" asp-for="Settings.EnableScreenCapture" />
                    <label class="form-check-label" asp-for="Settings.EnableScreenCapture">
                        Capture screenshots
                    </label>
                </div>
                
                <!-- More advanced settings... -->
            </div>
        </div>
    }
    
    <div class="d-flex justify-content-between">
        <a asp-page="/Devices/Details" asp-route-id="@Model.DeviceId" class="btn btn-outline-secondary">
            Cancel
        </a>
        <button type="submit" class="btn btn-primary">
            Save Settings
        </button>
    </div>
</form>
```

### Testing Checklist for Phase 5

- [ ] URL with ProfileId: `/Devices/UserSettings?DeviceId={id}&Username={name}&ProfileId={profile}` loads correctly
- [ ] URL without ProfileId: Shows "Basic Monitoring" mode
- [ ] URL with invalid ProfileId: Returns 404
- [ ] Breadcrumb shows correct path with child name when ProfileId present
- [ ] "Connect to child" banner shows only for unassigned logins
- [ ] Advanced settings disabled for unassigned logins
- [ ] Settings save correctly with ProfileId in database
- [ ] Different settings for same login with different ProfileIds work independently
- [ ] Configuration publish triggers correctly after save

---

## Phase 3: Shared UI Components {#phase-3}

**Priority**: MEDIUM - Reusable components for consistency
**Estimated Time**: 2 hours

### 3.1: User State Badge Component

**File**: `Pages/Shared/_UserStateBadgePartial.cshtml`

**Purpose**: Display visual badge for login state (Pending, Linked, Unassigned, Ignored)

**Implementation**:

```html
@model TGP.UserDashboard.Services.DTOs.DeviceUserState

@{
    var (badgeClass, icon, text) = Model switch
    {
        DeviceUserState.Pending => ("badge bg-warning text-dark", "bi-clock", "Pending Review"),
        DeviceUserState.Linked => ("badge bg-success", "bi-link-45deg", "Connected"),
        DeviceUserState.Unassigned => ("badge bg-secondary", "bi-question-circle", "Watching"),
        DeviceUserState.Ignored => ("badge bg-light text-muted", "bi-x-circle", "Skipped"),
        _ => ("badge bg-secondary", "", "Unknown")
    };
}

<span class="@badgeClass">
    @if (!string.IsNullOrEmpty(icon))
    {
        <i class="@icon"></i>
    }
    @text
</span>
```

**Usage**:
```html
<partial name="_UserStateBadgePartial" model="DeviceUserState.Pending" />
```

### 3.2: CSS for State Badges

**File**: `wwwroot/css/site.css`

**Add**:

```css
/* User State Badges */
.badge {
    font-weight: 500;
    padding: 0.375rem 0.75rem;
    border-radius: 0.375rem;
}

.badge i {
    margin-right: 0.25rem;
}

/* State-specific styling */
.badge.bg-warning {
    animation: pulse-warning 2s ease-in-out infinite;
}

@keyframes pulse-warning {
    0%, 100% {
        opacity: 1;
    }
    50% {
        opacity: 0.7;
    }
}
```

---

## Phase 4: Device Flow Pages {#phase-4}

**Priority**: HIGH - Primary user entry point
**Estimated Time**: 3 hours

### 4.1: Dashboard - Add Alert for New Logins

**File**: `Pages/Index.cshtml.cs`

**Add**: Query for pending detected users count

```csharp
public class IndexModel : PageModel
{
    private readonly IDetectedUserRepository _detectedUserRepository;
    private readonly TenantService _tenantService;
    
    public int PendingDetectedUsersCount { get; set; }
    
    public async Task OnGetAsync()
    {
        var userId = User.GetUserId();
        if (userId.HasValue)
        {
            var tenant = await _tenantService.GetOrCreateTenantAsync(userId.Value);
            
            // Get count of unreviewed detected users
            PendingDetectedUsersCount = await _detectedUserRepository
                .CountByTenantAsync(tenant.Id, reviewedOnly: false);
        }
    }
}
```

**File**: `Pages/Index.cshtml`

**Add Alert**:

```html
@if (Model.PendingDetectedUsersCount > 0)
{
    <div class="alert alert-warning alert-dismissible fade show" role="alert">
        <h5 class="alert-heading">
            <i class="bi bi-exclamation-triangle"></i> New Logins Found
        </h5>
        <p>
            We found <strong>@Model.PendingDetectedUsersCount</strong> new 
            @(Model.PendingDetectedUsersCount == 1 ? "login" : "logins") on your devices that need your attention.
        </p>
        <a asp-page="/Devices/DetectedUsers" class="btn btn-warning btn-sm">
            Review Now <i class="bi bi-arrow-right"></i>
        </a>
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
```

### 4.2: Devices/Index - Show User Counts

**File**: `Pages/Devices/Index.cshtml.cs`

**Update**: Query to include user statistics

```csharp
public class DeviceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public int PendingUsersCount { get; set; }
    public int LinkedUsersCount { get; set; }
    public int UnassignedUsersCount { get; set; }
}

public async Task OnGetAsync()
{
    // Existing code to get devices...
    
    // For each device, get user counts
    foreach (var device in Devices)
    {
        device.PendingUsersCount = await _context.DetectedUsers
            .Where(du => du.DeviceId == device.Id && !du.IsReviewed)
            .CountAsync();
            
        var monitoredUsers = await _context.MonitoredUsers
            .Where(mu => mu.DeviceId == device.Id)
            .ToListAsync();
            
        device.LinkedUsersCount = 0;
        device.UnassignedUsersCount = 0;
        
        foreach (var mu in monitoredUsers)
        {
            var hasLink = await _context.MonitoredProfileLinks
                .AnyAsync(l => l.DeviceId == device.Id && l.Username == mu.Username);
                
            if (hasLink)
                device.LinkedUsersCount++;
            else
                device.UnassignedUsersCount++;
        }
    }
}
```

**File**: `Pages/Devices/Index.cshtml`

**Update Card**:

```html
<div class="card-footer">
    <div class="d-flex justify-content-between align-items-center">
        <div>
            @if (device.PendingUsersCount > 0)
            {
                <span class="badge bg-warning text-dark">
                    <i class="bi bi-clock"></i> @device.PendingUsersCount new
                </span>
            }
            @if (device.LinkedUsersCount > 0)
            {
                <span class="badge bg-success">
                    <i class="bi bi-person-check"></i> @device.LinkedUsersCount connected
                </span>
            }
            @if (device.UnassignedUsersCount > 0)
            {
                <span class="badge bg-secondary">
                    <i class="bi bi-person"></i> @device.UnassignedUsersCount watching
                </span>
            }
        </div>
        <a asp-page="./Details" asp-route-id="@device.Id" class="btn btn-sm btn-outline-primary">
            Manage <i class="bi bi-arrow-right"></i>
        </a>
    </div>
</div>
```

---

## Phase 6: UnassignedUsers Page {#phase-6}

**File**: `Pages/Devices/UnassignedUsers.cshtml` (NEW)
**File**: `Pages/Devices/UnassignedUsers.cshtml.cs` (NEW)

**Purpose**: Dedicated page for managing all logins not connected to a child

**Page Model**:

```csharp
public class UnassignedUsersModel : PageModel
{
    public class UnassignedLoginDto
    {
        public Guid MonitoredUserId { get; set; }
        public Guid DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public DateTime FirstSeen { get; set; }
    }
    
    public List<UnassignedLoginDto> UnassignedLogins { get; set; } = new();
    public List<SelectListItem> ChildProfiles { get; set; } = new();
    
    public async Task OnGetAsync()
    {
        var userId = User.GetUserId();
        if (!userId.HasValue) return;
        
        var tenant = await _tenantService.GetOrCreateTenantAsync(userId.Value);
        
        // Get all monitored users
        var monitoredUsers = await _context.MonitoredUsers
            .Include(mu => mu.Device)
            .Where(mu => mu.Device.TenantId == tenant.Id)
            .ToListAsync();
        
        // Filter to only unassigned
        foreach (var mu in monitoredUsers)
        {
            var hasLink = await _context.MonitoredProfileLinks
                .AnyAsync(l => l.DeviceId == mu.DeviceId && l.Username == mu.Username);
                
            if (!hasLink)
            {
                UnassignedLogins.Add(new UnassignedLoginDto
                {
                    MonitoredUserId = mu.Id,
                    DeviceId = mu.DeviceId,
                    DeviceName = mu.Device.DeviceName,
                    Username = mu.Username,
                    FirstSeen = mu.CreatedAt
                });
            }
        }
        
        // Load child profiles for dropdown
        var profiles = await _childrenService.ListProfilesAsync(userId.Value, tenant.Id);
        ChildProfiles = profiles.Select(p => new SelectListItem 
        { 
            Value = p.Id.ToString(), 
            Text = p.Name 
        }).ToList();
    }
    
    public async Task<IActionResult> OnPostConnectAsync(Guid deviceId, string username, Guid profileId)
    {
        var userId = User.GetUserId();
        if (!userId.HasValue) return Unauthorized();
        
        var tenant = await _tenantService.GetOrCreateTenantAsync(userId.Value);
        
        // Create link
        await _childrenService.LinkAsync(profileId, deviceId, username, userId.Value, tenant.Id);
        
        return RedirectToPage();
    }
    
    public async Task<IActionResult> OnPostStopWatchingAsync(Guid monitoredUserId)
    {
        var monitoredUser = await _context.MonitoredUsers.FindAsync(monitoredUserId);
        if (monitoredUser == null) return NotFound();
        
        // Delete settings and user
        var settings = await _context.MonitoredUserSettings
            .Where(s => s.MonitoredUserId == monitoredUserId)
            .ToListAsync();
        _context.MonitoredUserSettings.RemoveRange(settings);
        _context.MonitoredUsers.Remove(monitoredUser);
        
        await _context.SaveChangesAsync();
        
        return RedirectToPage();
    }
}
```

**View**:

```html
@page
@model UnassignedUsersModel

@{
    ViewData["Title"] = "Logins I'm Watching";
}

<div class="d-flex justify-content-between align-items-center mb-4">
    <div>
        <h2>Logins I'm Watching</h2>
        <p class="text-muted mb-0">
            These logins are being monitored but not connected to a child profile yet.
        </p>
    </div>
</div>

@if (Model.UnassignedLogins.Any())
{
    <div class="card">
        <div class="card-body">
            <table class="table table-hover mb-0">
                <thead>
                    <tr>
                        <th>Login</th>
                        <th>Device</th>
                        <th>First Seen</th>
                        <th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var login in Model.UnassignedLogins)
                    {
                        <tr>
                            <td>
                                <strong>@login.Username</strong>
                                <br/>
                                <small class="text-muted">Not connected</small>
                            </td>
                            <td>@login.DeviceName</td>
                            <td>@login.FirstSeen.ToString("MMM d, yyyy")</td>
                            <td>
                                <div class="btn-group btn-group-sm">
                                    <button type="button" class="btn btn-outline-primary dropdown-toggle" 
                                            data-bs-toggle="dropdown">
                                        Connect to...
                                    </button>
                                    <ul class="dropdown-menu">
                                        @foreach (var profile in Model.ChildProfiles)
                                        {
                                            <li>
                                                <form method="post" asp-page-handler="Connect" 
                                                      asp-route-deviceId="@login.DeviceId" 
                                                      asp-route-username="@login.Username" 
                                                      asp-route-profileId="@profile.Value"
                                                      class="d-inline">
                                                    <button type="submit" class="dropdown-item">
                                                        @profile.Text
                                                    </button>
                                                </form>
                                            </li>
                                        }
                                    </ul>
                                </div>
                                
                                <a asp-page="/Devices/UserSettings" 
                                   asp-route-deviceId="@login.DeviceId" 
                                   asp-route-username="@login.Username"
                                   class="btn btn-sm btn-outline-secondary">
                                    Edit Rules
                                </a>
                                
                                <form method="post" asp-page-handler="StopWatching" 
                                      asp-route-monitoredUserId="@login.MonitoredUserId"
                                      class="d-inline"
                                      onsubmit="return confirm('Stop monitoring this login?');">
                                    <button type="submit" class="btn btn-sm btn-outline-danger">
                                        Stop Watching
                                    </button>
                                </form>
                            </td>
                        </tr>
                    }
                </tbody>
            </table>
        </div>
    </div>
}
else
{
    <div class="alert alert-info">
        <h5 class="alert-heading">All Caught Up!</h5>
        <p class="mb-0">
            All your monitored logins are connected to child profiles.
        </p>
    </div>
}
```

---

## Phase 7: Children Pages {#phase-7}

### 7.1: Children/Details - Show Device Logins

**File**: `Pages/Children/Details.cshtml`

**Add Section**:

```html
<div class="card mb-4">
    <div class="card-header">
        <h5 class="mb-0">@Model.Profile.Name's Logins</h5>
    </div>
    <div class="card-body">
        @if (Model.LinkedAccounts.Any())
        {
            <div class="list-group list-group-flush">
                @foreach (var login in Model.LinkedAccounts)
                {
                    <div class="list-group-item d-flex justify-content-between align-items-center">
                        <div>
                            <h6 class="mb-1">@login.Username</h6>
                            <small class="text-muted">
                                <i class="bi bi-@(login.DeviceType.ToLower())"></i> 
                                @login.DeviceName
                            </small>
                        </div>
                        <div class="btn-group btn-group-sm">
                            <a asp-page="/Devices/UserSettings" 
                               asp-route-deviceId="@login.DeviceId" 
                               asp-route-username="@login.Username"
                               asp-route-profileId="@Model.Profile.Id"
                               class="btn btn-outline-primary">
                                Manage Rules
                            </a>
                            <form method="post" asp-page-handler="UnlinkAccount" 
                                  asp-route-deviceId="@login.DeviceId"
                                  asp-route-username="@login.Username"
                                  class="d-inline"
                                  onsubmit="return confirm('Disconnect this login?');">
                                <button type="submit" class="btn btn-outline-danger">
                                    Disconnect
                                </button>
                            </form>
                        </div>
                    </div>
                }
            </div>
        }
        else
        {
            <p class="text-muted mb-0">
                No logins connected yet. 
                <a asp-page="/Devices/UnassignedUsers">Connect a login</a> to start monitoring.
            </p>
        }
    </div>
</div>
```

---

## Phase 8: Terminology Cleanup {#phase-8}

**Priority**: HIGH - Consistency is critical
**Estimated Time**: 1 hour

### Global Find & Replace

**Search For**: (Case-insensitive, whole words)
- "account" (in UI context - not authentication)
- "user account"
- "device account"

**Replace With**:
- "login"
- "login"  
- "login"

**Files to Update**:
- `Pages/Devices/Index.cshtml`
- `Pages/Devices/Details.cshtml`
- `Pages/Devices/DetectedUsers.cshtml`
- `Pages/Children/Index.cshtml`
- `Pages/Children/Details.cshtml`
- All `_Layout.cshtml` navigation text

### Specific Updates

**Navigation Menu** (`Pages/Shared/_Layout.cshtml`):

```html
<li class="nav-item">
    <a class="nav-link" asp-page="/Devices/UnassignedUsers">
        <i class="bi bi-person"></i> Logins I'm Watching
    </a>
</li>
```

**Page Titles**: Verify all use "Login" terminology

**Button Labels**: Update action buttons 
- "Set up for account" → "Connect login to..."
- "Account settings" → "Manage rules"
- "View account" → "View login"

---

## Phase 9: Final Polish {#phase-9}

### 9.1: Loading States

**Add to all pages with async operations**:

```html
<div id="loading-spinner" class="text-center py-5" style="display:none;">
    <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading...</span>
    </div>
</div>

<script>
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', () => {
            document.getElementById('loading-spinner').style.display = 'block';
        });
    });
</script>
```

### 9.2: Empty States

Ensure every list/table has a helpful empty state:

```html
@if (!Model.Items.Any())
{
    <div class="text-center py-5">
        <i class="bi bi-inbox display-1 text-muted"></i>
        <h4 class="mt-3">No Items Yet</h4>
        <p class="text-muted">
            [Helpful message explaining what to do]
        </p>
        <a asp-page="[NextStep]" class="btn btn-primary">
            Get Started
        </a>
    </div>
}
```

### 9.3: Error Handling

**Add to all forms**:

```html
@if (!string.IsNullOrEmpty(Model.ErrorMessage))
{
    <div class="alert alert-danger alert-dismissible fade show">
        <strong>Error:</strong> @Model.ErrorMessage
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}

@if (!string.IsNullOrEmpty(Model.SuccessMessage))
{
    <div class="alert alert-success alert-dismissible fade show">
        @Model.SuccessMessage
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
```

---

## Testing & Verification {#testing}

### Complete Test Flows

**Test Flow 1: New Device Login → Assign to Child → Configure**
1. Navigate to Dashboard
2. Verify "New logins found" alert appears
3. Click "Review Now"
4. See detected user "john_doe"
5. Click "Monitor & Assign"
6. Select child "Alice"
7. Verify redirect to UserSettings with ProfileId in URL
8. Verify breadcrumb shows "Device › john_doe › Rules for Alice"
9. Enable advanced settings
10. Save
11. Verify settings saved in database with correct ProfileId

**Test Flow 2: Unassigned Login Management**
1. Navigate to "Logins I'm Watching"
2. See "john_doe" listed as unassigned
3. Click "Connect to..." → "Alice"
4. Verify login now appears in Alice's child detail page
5. Click "Manage Rules" from Alice's page
6. Verify ProfileId parameter in URL
7. Make changes and save
8. Verify settings are specific to (MonitoredUser, Alice) combination

**Test Flow 3: Multiple Children, Same Login**
1. Create settings for john_doe + Alice
2. Connect john_doe to Bob as well
3. Navigate to john_doe settings with ProfileId=Alice
4. Set EnableScreenCapture = true, Save
5. Navigate to john_doe settings with ProfileId=Bob
6. Verify EnableScreenCapture = false (different settings)
7. Set EnableScreenCapture = false, Save
8. Return to Alice's settings
9. Verify EnableScreenCapture still = true

### Database Verification Queries

```sql
-- Verify unique constraint
SELECT MonitoredUserId, ProfileId, COUNT(*)
FROM MonitoredUserSettings
GROUP BY MonitoredUserId, ProfileId
HAVING COUNT(*) > 1;
-- Should return 0 rows

-- Verify NULL ProfileId settings exist
SELECT COUNT(*)
FROM MonitoredUserSettings
WHERE ProfileId IS NULL;
-- Should be > 0 for unassigned logins

-- Verify settings differentiation
SELECT mu.Username, p.DisplayName, mus.EnableScreenCapture
FROM MonitoredUserSettings mus
JOIN MonitoredUsers mu ON mus.MonitoredUserId = mu.Id
LEFT JOIN MonitoredProfiles p ON mus.ProfileId = p.Id
WHERE mu.Username = 'john_doe';
-- Should show different settings per child
```

---

## Quality Checklist {#quality-checklist}

### Before Committing Each Phase

**Code Quality**:
- [ ] No commented-out code
- [ ] Proper error handling on all async operations
- [ ] Loading states on all forms
- [ ] Validation on all inputs
- [ ] Null checks where appropriate
- [ ] Consistent naming conventions
- [ ] XML documentation on public methods

**UI/UX**:
- [ ] All terminology uses "Login" not "Account" (for device logins)
- [ ] Breadcrumbs accurate on every page
- [ ] Empty states on all lists/tables
- [ ] Success/error messages on all forms
- [ ] Responsive design (mobile-friendly)
- [ ] Accessible (ARIA labels, proper heading hierarchy)
- [ ] Consistent button styles and colors

**Data Integrity**:
- [ ] Tenant isolation verified
- [ ] ProfileId properly saved with settings
- [ ] Unique constraints respected
- [ ] Foreign keys configured correctly
- [ ] Cascade deletes handled appropriately

**Testing**:
- [ ] Local build succeeds
- [ ] All test flows pass
- [ ] Database queries return expected results
- [ ] No console errors in browser
- [ ] Configuration publish works

### Final Pre-Deployment Checklist

- [ ] All 9 phases complete
- [ ] All tests passing
- [ ] Code reviewed for quality
- [ ] Terminology 100% consistent
- [ ] No broken links
- [ ] All images/assets loading
- [ ] Build succeeds in CI/CD
- [ ] Deployment completed
- [ ] Live site smoke tested
- [ ] Walkthrough updated

---

## Commit Strategy

**Commit After Each Phase**:

Phase 5.1-5.3:
```bash
git add -A
git commit -m "feat(usersettings): implement ProfileId support

- Updated OnGetAsync to load settings by (MonitoredUserId, ProfileId)
- Updated OnPostAsync to save with correct ProfileId
- Added breadcrumb logic for assigned vs unassigned
- Added unassigned login banner
- Disabled advanced settings for unassigned logins

Completes Phase 5 - UserSettings now fully supports per-child configuration"
git push
```

Phase 3:
```bash
git commit -m "feat(components): add shared UI components

- Created _UserStateBadgePartial for visual state indicators
- Added CSS for state badges with animations
- Components ready for use across all pages

Completes Phase 3"
```

Continue pattern for remaining phases...

---

**End of Implementation Guide**

*This guide provides complete, production-ready code  and instructions for implementing Phases 3-9. Follow sequentially, test thoroughly at each step, and maintain the quality standard throughout.*
