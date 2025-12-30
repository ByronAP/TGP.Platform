# Requirements Document

## Introduction

This document specifies the requirements for a systematic audit of the TGP User Dashboard to ensure all pages and controls are properly connected to real live data sources. The audit will verify that no placeholder, mock, or hardcoded data remains in the UI, and that all interactive controls trigger appropriate backend operations with proper feedback.

## Glossary

- **User_Dashboard**: The ASP.NET Core Razor Pages web application providing the parent-facing interface for TGP
- **Data_Binding**: The connection between UI elements and backend data sources (services, APIs, database)
- **Control**: An interactive UI element such as buttons, forms, toggles, dropdowns, or links
- **Live_Data**: Real data retrieved from backend services, APIs, or database queries
- **Placeholder_Data**: Static, hardcoded, or mock data used during development
- **Page_Model**: The C# code-behind class for a Razor Page that handles data loading and form submissions
- **Service**: A backend class that provides data access or business logic (e.g., DeviceManagementService, AlertsService)
- **SignalR**: Real-time communication library for pushing updates to connected clients
- **SoftDeleted**: A tenant status indicating the account is pending permanent deletion after a 7-day retention period
- **SSO_Service**: The Single Sign-On microservice responsible for authentication, user management, and account lifecycle operations

## Requirements

---
## Part A: Infrastructure Connectivity
---

### Requirement 1: PostgreSQL Database Connectivity

**User Story:** As a system operator, I want the dashboard to connect to PostgreSQL reliably, so that all data operations function correctly.

#### Acceptance Criteria

1. WHEN the application starts THEN the User_Dashboard SHALL establish a connection to PostgreSQL using configured connection string
2. WHEN database queries are executed THEN the User_Dashboard SHALL use TgpDbContext with proper connection pooling
3. WHEN database connection fails THEN the User_Dashboard SHALL log the error and display a service unavailable message
4. WHEN multi-tenant queries are executed THEN the User_Dashboard SHALL filter data by the authenticated user's tenant

Note: Database migrations are handled by the TGP.DbMigrator service to avoid race conditions in scaled deployments.

### Requirement 2: Redis Cache and Session Connectivity

**User Story:** As a system operator, I want the dashboard to use Redis for caching and sessions, so that performance is optimized and sessions are distributed.

#### Acceptance Criteria

1. WHEN the application starts THEN the User_Dashboard SHALL establish a connection to Redis using configured connection string
2. WHEN session data is stored THEN the User_Dashboard SHALL persist to Redis for distributed session support
3. WHEN cached data is requested THEN the User_Dashboard SHALL retrieve from Redis cache before database
4. WHEN Redis connection fails THEN the User_Dashboard SHALL fall back gracefully and log the error
5. WHEN SignalR backplane is configured THEN the User_Dashboard SHALL use Redis for message distribution

### Requirement 3: RabbitMQ Message Bus Connectivity

**User Story:** As a system operator, I want the dashboard to publish messages to RabbitMQ, so that device commands and configuration updates are processed asynchronously.

#### Acceptance Criteria

1. WHEN the application starts THEN the User_Dashboard SHALL establish a connection to RabbitMQ using configured connection string
2. WHEN a device command is created THEN the User_Dashboard SHALL publish a message to the tgp.devices exchange
3. WHEN a configuration update is saved THEN the User_Dashboard SHALL publish a message to the tgp.config exchange
4. WHEN RabbitMQ connection fails THEN the User_Dashboard SHALL queue messages locally and retry with exponential backoff
5. WHEN message publishing succeeds THEN the User_Dashboard SHALL log the message ID for traceability

### Requirement 4: S3/MinIO Storage Connectivity

**User Story:** As a system operator, I want the dashboard to access S3-compatible storage, so that screenshots and files can be retrieved and displayed.

#### Acceptance Criteria

1. WHEN the application starts THEN the User_Dashboard SHALL verify S3/MinIO connectivity using configured credentials
2. WHEN a screenshot URL is requested THEN the User_Dashboard SHALL generate a pre-signed URL with appropriate expiration
3. WHEN S3 connection fails THEN the User_Dashboard SHALL display placeholder images and log the error
4. WHEN listing bucket contents THEN the User_Dashboard SHALL paginate results and handle large datasets

Note: The dashboard only reads from S3 (viewing screenshots/logs). File uploads are handled by device agents via the Device Gateway.

### Requirement 5: SSO API Connectivity

**User Story:** As a system operator, I want the dashboard to communicate with the SSO microservice, so that authentication and user management function correctly.

#### Acceptance Criteria

1. WHEN authentication is requested THEN the User_Dashboard SHALL call the SSO API login endpoint with credentials
2. WHEN token refresh is needed THEN the User_Dashboard SHALL call the SSO API refresh endpoint automatically
3. WHEN user data is requested THEN the User_Dashboard SHALL call the SSO API with the Bearer token
4. WHEN SSO API returns an error THEN the User_Dashboard SHALL display appropriate error messages to the user
5. WHEN SSO API is unavailable THEN the User_Dashboard SHALL display a service unavailable message and retry

### Requirement 6: Health Check and Diagnostics

**User Story:** As a system operator, I want health checks for all dependencies, so that I can monitor system status and troubleshoot issues.

#### Acceptance Criteria

1. WHEN the health endpoint is called THEN the User_Dashboard SHALL report status of PostgreSQL, Redis, RabbitMQ, and S3
2. WHEN any dependency is unhealthy THEN the User_Dashboard SHALL report degraded status with specific failure details
3. WHEN all dependencies are healthy THEN the User_Dashboard SHALL report healthy status
4. WHEN diagnostics are requested THEN the User_Dashboard SHALL provide connection latency metrics for each dependency

Note: The Device Gateway is not a direct dependency of the dashboard. Device data is read from the shared PostgreSQL database, which is populated by the Device Gateway service.

---
## Part B: Authentication & Authorization
---

### Requirement 8: Login and Authentication Data Binding

**User Story:** As a user, I want to log in securely with my credentials and complete MFA when required, so that I can access my account safely.

#### Acceptance Criteria

1. WHEN the Login page is submitted with valid credentials THEN the User_Dashboard SHALL authenticate via SSO API and receive tokens
2. WHEN authentication succeeds without MFA THEN the User_Dashboard SHALL set the TGP_Token cookie and redirect to the dashboard
3. WHEN authentication requires MFA THEN the User_Dashboard SHALL redirect to the VerifyMfa page with the appropriate session state
4. WHEN the VerifyMfa page is submitted with a valid code THEN the User_Dashboard SHALL complete authentication and redirect to the dashboard
5. WHEN the VerifyMfa page is submitted with an invalid code THEN the User_Dashboard SHALL display an error and allow retry
6. WHEN login fails due to invalid credentials THEN the User_Dashboard SHALL display an error message without revealing which field is incorrect
7. WHEN login fails due to account lockout THEN the User_Dashboard SHALL display a lockout message with remaining time

### Requirement 9: Registration Data Binding

**User Story:** As a new user, I want to register an account with proper validation, so that I can start using the service.

#### Acceptance Criteria

1. WHEN the Register page is submitted with valid data THEN the User_Dashboard SHALL create the account via SSO API
2. WHEN registration succeeds THEN the User_Dashboard SHALL redirect to login or verification flow as appropriate
3. WHEN registration fails due to duplicate email THEN the User_Dashboard SHALL display an appropriate error message
4. WHEN registration fails due to password requirements THEN the User_Dashboard SHALL display specific password policy violations
5. WHEN terms acceptance is not checked THEN the User_Dashboard SHALL prevent submission and indicate the requirement
6. WHEN parental consent is required THEN the User_Dashboard SHALL display and enforce consent collection

### Requirement 10: Logout Data Binding

**User Story:** As a user, I want to log out securely, so that my session is properly terminated.

#### Acceptance Criteria

1. WHEN the Logout action is triggered THEN the User_Dashboard SHALL clear the TGP_Token cookie
2. WHEN the Logout action completes THEN the User_Dashboard SHALL invalidate the refresh token via SSO API
3. WHEN the Logout action completes THEN the User_Dashboard SHALL redirect to the login page
4. WHEN logout fails due to API error THEN the User_Dashboard SHALL still clear local cookies and redirect

---
## Part C: Real-Time Communication
---

### Requirement 11: SignalR Real-Time Updates

**User Story:** As a parent, I want to receive real-time updates on the dashboard without refreshing, so that I can see activity as it happens.

#### Acceptance Criteria

1. WHEN the Dashboard page loads THEN the User_Dashboard SHALL establish a SignalR connection to the TimelineHub
2. WHEN a new alert is detected THEN the User_Dashboard SHALL push the alert to connected clients via SignalR
3. WHEN a new alert arrives via SignalR THEN the User_Dashboard SHALL display a notification and update the alerts list
4. WHEN device status changes THEN the User_Dashboard SHALL push the update to connected clients via SignalR
5. WHEN device status update arrives via SignalR THEN the User_Dashboard SHALL update the device card without page refresh
6. WHEN a command status changes (pending, acknowledged, completed) THEN the User_Dashboard SHALL push the update via SignalR
7. WHEN command status update arrives via SignalR THEN the User_Dashboard SHALL update the command feedback UI
8. WHEN a new timeline event occurs THEN the User_Dashboard SHALL push the event to the Timeline page via SignalR
9. WHEN the SignalR connection is lost THEN the User_Dashboard SHALL display a connection status indicator
10. WHEN the SignalR connection is lost THEN the User_Dashboard SHALL attempt automatic reconnection with exponential backoff
11. WHEN SignalR reconnection succeeds THEN the User_Dashboard SHALL fetch missed events and update the UI
12. WHEN multiple clients are connected THEN the User_Dashboard SHALL use Redis backplane for SignalR message distribution

### Requirement 12: Real-Time Alert Notifications

**User Story:** As a parent, I want to be notified immediately when concerning activity is detected, so that I can respond quickly.

#### Acceptance Criteria

1. WHEN a critical alert is received via SignalR THEN the User_Dashboard SHALL display a prominent notification banner
2. WHEN an alert notification is displayed THEN the User_Dashboard SHALL include alert severity, device name, and summary
3. WHEN the user clicks an alert notification THEN the User_Dashboard SHALL navigate to the relevant device or timeline
4. WHEN the user dismisses an alert notification THEN the User_Dashboard SHALL remove the banner and mark as seen
5. WHEN multiple alerts arrive in quick succession THEN the User_Dashboard SHALL batch notifications to avoid overwhelming the user

---
## Part D: Main Pages Data Binding
---

### Requirement 13: Home Page Data Binding

**User Story:** As a parent, I want the home page to show a summary of my family's monitoring status, so that I can quickly assess the situation.

#### Acceptance Criteria

1. WHEN the Home page (Index) loads THEN the User_Dashboard SHALL display device count from the database
2. WHEN the Home page loads THEN the User_Dashboard SHALL display today's alert count from AlertsService
3. WHEN no devices are registered THEN the User_Dashboard SHALL display onboarding guidance with Add Device CTA
4. WHEN devices exist THEN the User_Dashboard SHALL display quick action buttons (Add Device, Create Child Profile)
5. WHEN the user is not verified THEN the User_Dashboard SHALL display a verification banner with appropriate messaging

### Requirement 14: Dashboard Page Data Binding

**User Story:** As a parent, I want the dashboard to display my actual devices and alerts, so that I can monitor my family's digital activity accurately.

#### Acceptance Criteria

1. WHEN the Dashboard/Index page loads THEN the User_Dashboard SHALL display devices retrieved from DeviceManagementService for the authenticated user
2. WHEN the Dashboard/Index page loads THEN the User_Dashboard SHALL display recent alerts retrieved from AlertsService for the authenticated user's devices
3. WHEN a device card is clicked THEN the User_Dashboard SHALL navigate to the device details page with the correct device identifier
4. WHEN no devices exist for the user THEN the User_Dashboard SHALL display an appropriate empty state with onboarding guidance
5. WHEN data loading fails THEN the User_Dashboard SHALL display an error message and provide retry capability

### Requirement 15: Timeline Page Data Binding

**User Story:** As a parent, I want to view a real-time timeline of activity, so that I can see what is happening across all monitored devices.

#### Acceptance Criteria

1. WHEN the Timeline/Index page loads THEN the User_Dashboard SHALL display aggregated events (alerts, heartbeats, commands) from TimelineService
2. WHEN filters are applied (device, date range, type) THEN the User_Dashboard SHALL query filtered data and update the display
3. WHEN new events arrive via SignalR THEN the User_Dashboard SHALL append events to the timeline without page refresh
4. WHEN a screenshot thumbnail is displayed THEN the User_Dashboard SHALL use signed URLs from StorageService
5. WHEN the SignalR connection is lost THEN the User_Dashboard SHALL display a reconnection indicator and attempt reconnection

---
## Part E: Device Management Pages
---

### Requirement 16: Devices Index Page Data Binding

**User Story:** As a parent, I want to view and manage my devices with real-time information, so that I can configure monitoring settings effectively.

#### Acceptance Criteria

1. WHEN the Devices/Index page loads THEN the User_Dashboard SHALL display all devices for the authenticated user with current status from Live_Data
2. WHEN a quick action button (Lock, Sync) is clicked THEN the User_Dashboard SHALL create a DeviceCommand and publish to RabbitMQ
3. WHEN a command is submitted THEN the User_Dashboard SHALL display feedback indicating command status
4. WHEN no devices exist THEN the User_Dashboard SHALL display onboarding guidance
5. WHEN device status updates arrive via SignalR THEN the User_Dashboard SHALL update device cards in real-time

### Requirement 17: Device Details Page Data Binding

**User Story:** As a parent, I want to see detailed information about a specific device, so that I can understand its configuration and users.

#### Acceptance Criteria

1. WHEN the Devices/Details page loads THEN the User_Dashboard SHALL display device metadata (name, type, last seen, status) from the database
2. WHEN the Devices/Details page loads THEN the User_Dashboard SHALL display monitored users associated with the device from Live_Data
3. WHEN the Devices/Details page loads THEN the User_Dashboard SHALL display device configuration from DeviceManagementService
4. WHEN configuration is updated THEN the User_Dashboard SHALL save to database and publish to RabbitMQ
5. WHEN configuration save succeeds THEN the User_Dashboard SHALL display confirmation feedback

### Requirement 18: Device Commands Page Data Binding

**User Story:** As a parent, I want to send commands to devices and see their status, so that I can manage devices remotely.

#### Acceptance Criteria

1. WHEN the Devices/Commands page loads THEN the User_Dashboard SHALL display pending and recent commands from the database
2. WHEN a command is submitted THEN the User_Dashboard SHALL create a DeviceCommand record via DeviceManagementService
3. WHEN a command is created THEN the User_Dashboard SHALL publish to RabbitMQ via IMessagePublisher
4. WHEN command status updates arrive via SignalR THEN the User_Dashboard SHALL update the command list in real-time
5. WHEN a command expires without acknowledgement THEN the User_Dashboard SHALL display expired status

### Requirement 19: Detected Users Page Data Binding

**User Story:** As a parent, I want to review users detected on my devices, so that I can decide whether to monitor or ignore them.

#### Acceptance Criteria

1. WHEN the DetectedUsers page loads THEN the User_Dashboard SHALL display users detected on devices from the database
2. WHEN the Monitor action is performed THEN the User_Dashboard SHALL create a MonitoredUser record and display confirmation
3. WHEN the Ignore action is performed THEN the User_Dashboard SHALL add user to ignored list and display confirmation
4. WHEN the Link to Profile action is performed THEN the User_Dashboard SHALL associate the user with a child profile
5. WHEN an action completes THEN the User_Dashboard SHALL update the ReviewedBy and ReviewedAt audit fields

### Requirement 20: Device User Settings Page Data Binding

**User Story:** As a parent, I want to configure monitoring settings for specific users on a device, so that I can customize protection per child.

#### Acceptance Criteria

1. WHEN the Devices/UserSettings page loads THEN the User_Dashboard SHALL display current settings for the specified user from the database
2. WHEN monitoring toggles are changed THEN the User_Dashboard SHALL update MonitoredUserSettings in the database
3. WHEN settings are saved THEN the User_Dashboard SHALL publish configuration update to RabbitMQ
4. WHEN settings save succeeds THEN the User_Dashboard SHALL display confirmation feedback
5. WHEN settings save fails THEN the User_Dashboard SHALL display error message and preserve form state

---
## Part F: Children & Family Management
---

### Requirement 21: Children Index Page Data Binding

**User Story:** As a parent, I want to manage child profiles with real data, so that I can organize monitoring by family member.

#### Acceptance Criteria

1. WHEN the Children/Index page loads THEN the User_Dashboard SHALL display child profiles (MonitoredProfiles) from the database
2. WHEN the Children/Create form is submitted THEN the User_Dashboard SHALL create a new profile in the database
3. WHEN profile creation succeeds THEN the User_Dashboard SHALL refresh the list and display confirmation
4. WHEN no profiles exist THEN the User_Dashboard SHALL display empty state with create guidance
5. WHEN validation fails THEN the User_Dashboard SHALL display errors and preserve form state

### Requirement 22: Children Details Page Data Binding

**User Story:** As a parent, I want to view and manage a child profile's linked accounts, so that I can track their activity across devices.

#### Acceptance Criteria

1. WHEN the Children/Details page loads THEN the User_Dashboard SHALL display the profile with linked accounts from Live_Data
2. WHEN an account is linked THEN the User_Dashboard SHALL create a MonitoredProfileLink and refresh the list
3. WHEN an account is unlinked THEN the User_Dashboard SHALL delete the link and refresh the list
4. WHEN profile is renamed THEN the User_Dashboard SHALL update the database and display confirmation
5. WHEN profile is deleted THEN the User_Dashboard SHALL remove from database and redirect to index

### Requirement 23: Children Settings Page Data Binding

**User Story:** As a parent, I want to customize a child profile's appearance and settings, so that I can personalize the experience.

#### Acceptance Criteria

1. WHEN the Children/Settings page loads THEN the User_Dashboard SHALL display current settings from the database
2. WHEN color or avatar is changed THEN the User_Dashboard SHALL update the profile in the database
3. WHEN settings are saved THEN the User_Dashboard SHALL display confirmation feedback
4. WHEN settings save fails THEN the User_Dashboard SHALL display error message and preserve form state

---
## Part G: Account Management
---

### Requirement 24: Account Profile Page Data Binding

**User Story:** As a user, I want my profile page to show my account information and MFA status, so that I can manage my security settings.

#### Acceptance Criteria

1. WHEN the Account/Profile page loads THEN the User_Dashboard SHALL display the authenticated user's information from claims
2. WHEN the Account/Profile page loads THEN the User_Dashboard SHALL display MFA configuration status from SSO API
3. WHEN MFA enable is requested THEN the User_Dashboard SHALL call SSO API and display QR code or SMS setup
4. WHEN MFA verification code is submitted THEN the User_Dashboard SHALL verify via SSO API and update status
5. WHEN MFA disable is requested THEN the User_Dashboard SHALL call SSO API and update configuration display
6. WHEN backup codes regeneration is requested THEN the User_Dashboard SHALL call SSO API and display new codes

### Requirement 25: Account Settings Page Data Binding

**User Story:** As a user, I want to manage my preferences, so that I can customize my dashboard experience.

#### Acceptance Criteria

1. WHEN the Account/Settings page loads THEN the User_Dashboard SHALL display current user preferences from UserPreferencesService
2. WHEN theme preference is changed THEN the User_Dashboard SHALL persist to database via UserPreferencesService
3. WHEN theme is saved THEN the User_Dashboard SHALL update the TGP_Theme cookie immediately
4. WHEN settings save succeeds THEN the User_Dashboard SHALL display confirmation feedback
5. WHEN settings save fails THEN the User_Dashboard SHALL display error message

### Requirement 26: Account Deletion Data Binding

**User Story:** As a user, I want to delete my account with a recovery period, so that I can leave the service while having time to reconsider.

#### Acceptance Criteria

1. WHEN the user requests account deletion THEN the User_Dashboard SHALL call the SSO API delete-account endpoint to initiate a soft delete
2. WHEN account deletion is initiated successfully THEN the User_Dashboard SHALL display confirmation with the deletion effective date (7 days from request)
3. WHEN a user with a SoftDeleted tenant attempts to log in THEN the User_Dashboard SHALL reject the login and display a message explaining the account is pending deletion
4. WHEN a user with a SoftDeleted tenant attempts to log in THEN the User_Dashboard SHALL display an option to contact support to restore the account
5. WHEN account deletion is requested THEN the User_Dashboard SHALL require password confirmation before calling the SSO API
6. WHEN account deletion confirmation fails THEN the User_Dashboard SHALL display an error message and preserve the current state
7. WHEN the SSO API returns an error during deletion THEN the User_Dashboard SHALL display the error message and allow retry

---
## Part H: Tenant & Billing
---

### Requirement 27: Tenant Index Page Data Binding

**User Story:** As a family administrator, I want to manage tenant membership with real data, so that I can control who has access to family monitoring.

#### Acceptance Criteria

1. WHEN the Tenant/Index page loads THEN the User_Dashboard SHALL display tenant members and roles from TenantService
2. WHEN no tenant exists THEN the User_Dashboard SHALL create a default tenant for the user
3. WHEN a member remove action is performed THEN the User_Dashboard SHALL update the database and refresh the member list
4. WHEN the user lacks permission for an action THEN the User_Dashboard SHALL display appropriate error message

### Requirement 28: Tenant Invite Page Data Binding

**User Story:** As a family administrator, I want to invite others to my family tenant, so that they can help monitor children.

#### Acceptance Criteria

1. WHEN the Tenant/Invite page is submitted THEN the User_Dashboard SHALL create an invitation via TenantService
2. WHEN the invited user exists THEN the User_Dashboard SHALL add them directly to the tenant
3. WHEN the invited user does not exist THEN the User_Dashboard SHALL send an invitation email
4. WHEN invitation succeeds THEN the User_Dashboard SHALL display confirmation and redirect

### Requirement 29: Billing Index Page Data Binding

**User Story:** As a subscriber, I want billing pages to show my actual subscription status, so that I can manage my plan and payments.

#### Acceptance Criteria

1. WHEN the Billing/Index page loads THEN the User_Dashboard SHALL display current subscription status from SSO API
2. WHEN the Billing/Index page loads THEN the User_Dashboard SHALL display available plans from SSO API
3. WHEN a plan cancellation is requested THEN the User_Dashboard SHALL process via SSO API and update status
4. WHEN cancellation succeeds THEN the User_Dashboard SHALL display confirmation message

### Requirement 30: Billing Checkout Page Data Binding

**User Story:** As a user, I want to purchase or upgrade my subscription, so that I can access premium features.

#### Acceptance Criteria

1. WHEN the Billing/Checkout page loads THEN the User_Dashboard SHALL display selected plan details
2. WHEN checkout form is submitted THEN the User_Dashboard SHALL process payment via payment provider
3. WHEN payment succeeds THEN the User_Dashboard SHALL create subscription record and redirect to billing index
4. WHEN payment fails THEN the User_Dashboard SHALL display error message and preserve form state

---
## Part I: Reporting
---

### Requirement 31: Reporting Page Data Binding

**User Story:** As a parent, I want reports to show actual usage data, so that I can understand my family's digital habits.

#### Acceptance Criteria

1. WHEN the Dashboard/Report page loads THEN the User_Dashboard SHALL display charts with data from the Reporting API
2. WHEN date or device filters are changed THEN the User_Dashboard SHALL fetch filtered data and update charts
3. WHEN export is requested THEN the User_Dashboard SHALL generate a file with real data in the requested format
4. WHEN no data exists for the selected period THEN the User_Dashboard SHALL display an appropriate empty state

---
## Part J: Navigation & Forms
---

### Requirement 32: Navigation and Layout Data Binding

**User Story:** As a user, I want the navigation to reflect my current state, so that I can understand my context and access features appropriately.

#### Acceptance Criteria

1. WHEN the layout renders THEN the User_Dashboard SHALL display the authenticated user's name in the header from claims
2. WHEN the sidebar renders THEN the User_Dashboard SHALL highlight the current page based on the route
3. WHEN the theme toggle is changed THEN the User_Dashboard SHALL persist the preference and apply the theme immediately
4. WHEN verification status affects feature access THEN the User_Dashboard SHALL display appropriate gating UI based on claims

### Requirement 33: Form Validation and Error Handling

**User Story:** As a user, I want forms to validate input and show errors clearly, so that I can correct mistakes before submission.

#### Acceptance Criteria

1. WHEN a form is submitted with invalid data THEN the User_Dashboard SHALL display validation errors adjacent to the relevant fields
2. WHEN a server error occurs during form submission THEN the User_Dashboard SHALL display an error message and preserve form state
3. WHEN a form submission succeeds THEN the User_Dashboard SHALL display confirmation and navigate or refresh appropriately
4. WHEN required fields are empty THEN the User_Dashboard SHALL prevent submission and indicate required fields
