# Implementation Plan

## Phase 1: Infrastructure Connectivity Verification (Foundation)

- [x] 1. Verify Infrastructure Connections
  - [x] 1.1 Verify PostgreSQL connectivity in Program.cs
    - Confirm TgpDbContext is properly configured
    - Verify connection string is loaded from configuration
    - Verify connection pooling is enabled
    - _Requirements: 1.1, 1.2_
  - [x] 1.2 Verify database connectivity check at startup
    - Confirm connection failure logs error and shows service unavailable
    - Note: Migrations are handled by TGP.DbMigrator service (not dashboard)
    - _Requirements: 1.3_
  - [x] 1.3 Verify Redis connectivity for sessions and cache
    - Confirm distributed cache is configured
    - Verify session storage uses Redis
    - Verify cache retrieval before database via CacheService
    - _Requirements: 2.1, 2.2, 2.3_
  - [x] 1.4 Verify Redis fallback and SignalR backplane
    - Confirm graceful fallback when Redis unavailable
    - Verify SignalR uses Redis backplane for message distribution
    - _Requirements: 2.4, 2.5_
  - [x] 1.5 Verify RabbitMQ connectivity for messaging
    - Confirm IMessagePublisher is registered
    - Verify message publishing to tgp.devices and tgp.config exchanges
    - _Requirements: 3.1, 3.2, 3.3_
  - [x] 1.6 Verify RabbitMQ error handling and logging
    - Confirm local queuing and retry with exponential backoff on failure
    - Verify message ID is logged for traceability
    - _Requirements: 3.4, 3.5_
  - [x] 1.7 Verify S3/MinIO connectivity for storage (read-only)
    - Confirm StorageService is configured
    - Verify pre-signed URL generation with expiration for viewing screenshots
    - Note: Dashboard only reads from S3; uploads are done by device agents
    - _Requirements: 4.1, 4.2_
  - [x] 1.8 Verify S3 error handling and pagination
    - Confirm placeholder images on connection failure
    - Verify bucket listing pagination for large datasets
    - _Requirements: 4.4, 4.5_
  - [x] 1.9 Verify SSO API connectivity
    - Confirm TgpApiClient is configured with correct URLs
    - Verify authentication, token refresh, and user data endpoints
    - _Requirements: 5.1, 5.2, 5.3_
  - [x] 1.10 Verify SSO API error handling
    - Confirm appropriate error messages on API errors
    - Verify service unavailable message and retry on unavailability
    - _Requirements: 5.4, 5.5_

- [x] 2. Implement Health Checks





  - [x] 2.1 Add health check endpoint for all dependencies


    - Add PostgreSQL health check
    - Add Redis health check
    - Add RabbitMQ health check
    - Add S3 health check
    - _Requirements: 7.1, 7.2, 7.3_
  - [x] 2.2 Add diagnostics endpoint with latency metrics


    - Provide connection latency for each dependency
    - _Requirements: 7.4_
  - [ ]* 2.3 Write integration tests for infrastructure connectivity
    - Test database connection and error handling
    - Test Redis connection and fallback
    - Test RabbitMQ connection and retry
    - Test S3 connection and error handling
    - Test SSO API connection and error handling
    - _Requirements: 1.1, 1.3, 2.1, 2.4, 3.1, 3.4, 4.1, 4.4, 5.1, 5.4_

- [ ] 3. Checkpoint - Infrastructure connectivity verified
  - Ensure all tests pass, ask the user if questions arise.

## Phase 2: Tenant Data Isolation Verification (Security Foundation)

- [x] 4. Verify Tenant Data Isolation Across Services





  - [x] 4.1 Audit DeviceManagementService for tenant filtering


    - Verify all queries filter by TenantId
    - Add tenant filter if missing
    - _Requirements: 1.5, 14.1, 16.1_
  - [x] 4.2 Audit AlertsService for tenant filtering


    - Verify all queries filter by TenantId
    - Add tenant filter if missing
    - _Requirements: 1.5_
  - [x] 4.3 Audit TimelineService for tenant filtering


    - Verify all queries filter by TenantId
    - Add tenant filter if missing
    - _Requirements: 1.5_
  - [x] 4.4 Audit ChildrenService for tenant filtering


    - Verify all queries filter by TenantId/OwnerUserId
    - Add tenant filter if missing
    - _Requirements: 1.5_
  - [x] 4.5 Audit TenantService for tenant filtering


    - Verify all queries filter by TenantId
    - Add tenant filter if missing
    - _Requirements: 1.5_
  - [ ]* 4.6 Write property test for tenant data isolation
    - **Property 1: Tenant Data Isolation**
    - **Validates: Requirements 1.5, 14.1, 16.1**

- [ ] 5. Checkpoint - Tenant isolation verified
  - Ensure all tests pass, ask the user if questions arise.

## Phase 3: Database Schema Updates for Account Deletion

- [x] 6. Update Tenant Entity for Soft Delete Support





  - [x] 6.1 Add soft delete fields to Tenant entity in TGP.Data


    - Add `Status` property (string: "Active" or "SoftDeleted")
    - Add `SoftDeletedAt` property (DateTime?)
    - Add `DeletionEffectiveDate` property (DateTime?)
    - _Requirements: 26.1, 26.2_
  - [x] 6.2 Create database migration for Tenant soft delete fields


    - Generate EF Core migration
    - Add default value "Active" for Status column
    - _Requirements: 26.1_
  - [ ]* 6.3 Write property test for deletion effective date calculation
    - **Property 2: Deletion Effective Date Calculation**
    - **Validates: Requirements 26.2**

## Phase 4: SSO Microservice Account Deletion Implementation

- [x] 7. Implement Account Deletion Endpoint in SSO Microservice




  - [x] 7.1 Create DeleteAccountRequest and DeleteAccountResponse DTOs in SSO


    - DeleteAccountRequest: Password field for verification
    - DeleteAccountResponse: Success, DeletionEffectiveDate, ErrorMessage
    - _Requirements: 26.1, 26.5_
  - [x] 7.2 Implement DELETE /api/Auth/delete-account endpoint in SSO AuthController


    - Verify password against stored hash
    - Set Tenant.Status to "SoftDeleted"
    - Set Tenant.SoftDeletedAt to current UTC time
    - Set Tenant.DeletionEffectiveDate to SoftDeletedAt + 7 days
    - Return success with deletion effective date
    - Invalidate any and all current user tokens (not device)
    - _Requirements: 26.1, 26.2, 26.5_
  - [x] 7.3 Update SSO login endpoint to return tenant status


    - Add TenantStatus, SoftDeletedAt, DeletionEffectiveDate to login response
    - Return these fields when tenant is SoftDeleted
    - _Requirements: 26.3_
  - [x] 7.4 Implement login rejection for SoftDeleted tenants in SSO


    - Check tenant status during login
    - Return specific error code/message for SoftDeleted tenants
    - Include deletion effective date in response
    - _Requirements: 26.3_
  - [ ]* 7.5 Write unit tests for SSO account deletion endpoint
    - Test successful deletion with valid password
    - Test rejection with invalid password
    - Test login rejection for SoftDeleted tenant
    - _Requirements: 26.1, 26.3, 26.5_

## Phase 5: Dashboard API Integration for Account Deletion

- [x] 8. Extend TgpApiClient for Account Deletion





  - [x] 8.1 Add account deletion DTOs to TgpApiClient.cs


    - Add `AccountDeletionResult` class
    - Add `TenantStatusResult` class
    - Extend `LoginResult` with tenant status fields
    - _Requirements: 26.1, 26.3_
  - [x] 8.2 Add DeleteAccountAsync method to ITgpApiClient interface and implementation


    - POST to SSO API `/api/Auth/delete-account` endpoint
    - Accept password parameter for verification
    - Return `AccountDeletionResult` with success/error and effective date
    - _Requirements: 26.1, 26.5, 26.7_
  - [x] 8.3 Update LoginAsync to handle SoftDeleted tenant status


    - Parse tenant status from SSO API response
    - Set `TenantSoftDeleted` flag when status is "SoftDeleted"
    - Populate `SoftDeletedAt` and `DeletionEffectiveDate` fields
    - _Requirements: 26.3_
  - [ ]* 8.4 Write property test for SoftDeleted tenant login rejection
    - **Property 3: SoftDeleted Tenant Login Rejection**
    - **Validates: Requirements 26.3, 26.4**


## Phase 6: Account Deletion UI

- [x] 9. Create Account Deletion Pages





  - [x] 9.1 Create DeleteAccount.cshtml page


    - Add password confirmation form
    - Display warning about 7-day retention period
    - Include cancel button to return to settings
    - _Requirements: 26.1, 26.5, 26.6_
    - [x] 9.2 Create DeleteAccount.cshtml.cs page model


    - Implement OnPostAsync to call TgpApiClient.DeleteAccountAsync
    - Validate password before API call
    - Handle success: clear cookies, redirect to login with message
    - Handle failure: display error, preserve form state
    - _Requirements: 26.1, 26.5, 26.6, 26.7_
  - [x] 9.3 Create AccountDeleted.cshtml page


    - Display "Account pending deletion" message
    - Show deletion effective date
    - Include "Contact Support" button/link
    - _Requirements: 26.3, 26.4_
  - [x] 9.4 Create AccountDeleted.cshtml.cs page model


    - Accept deletion info via query parameters or TempData
    - Display support contact information
    - _Requirements: 26.3, 26.4_
  - [ ]* 9.5 Write unit tests for DeleteAccount page model
    - Test successful deletion flow
    - Test password validation failure
    - Test API error handling
    - _Requirements: 26.5, 26.6, 26.7_

## Phase 7: Login Flow Updates for Account Deletion

- [x] 10. Update Login Page for SoftDeleted Handling






  - [x] 10.1 Modify Login.cshtml.cs to check tenant status

    - After successful authentication, check `TenantSoftDeleted` flag
    - If SoftDeleted, redirect to AccountDeleted page instead of dashboard
    - Pass deletion info to AccountDeleted page
    - _Requirements: 26.3, 26.4_
  - [ ]* 10.2 Write unit tests for login with SoftDeleted tenant
    - Test redirect to AccountDeleted page
    - Test that cookies are NOT set for SoftDeleted tenants
    - _Requirements: 26.3_

## Phase 8: Account Settings Integration

- [x] 11. Add Delete Account Option to Settings Page






  - [x] 11.1 Update Account/Settings.cshtml to include delete account section

    - Add "Delete Account" section at bottom of page
    - Include warning text about permanent deletion
    - Add button linking to DeleteAccount page
    - _Requirements: 26.1_
  - [ ]* 11.2 Write unit tests for settings page delete account link
    - Test link renders correctly
    - Test navigation to DeleteAccount page
    - _Requirements: 26.1_

- [x] 12. Checkpoint - Account deletion feature complete





  - Ensure all tests pass, ask the user if questions arise.

## Phase 9: Authentication Pages Data Binding Audit

- [x] 13. Audit Login Page Data Binding





  - [x] 13.1 Verify Login.cshtml.cs calls SSO API correctly

    - Confirm LoginAsync is called with credentials
    - Verify token cookie is set on success
    - Verify MFA redirect works correctly
    - _Requirements: 8.1, 8.2, 8.3_
  - [x] 13.2 Verify error handling on Login page


    - Confirm invalid credentials show generic error
    - Confirm lockout message displays with remaining time
    - _Requirements: 8.6, 8.7_

- [x] 14. Audit Registration Page Data Binding




  - [x] 14.1 Verify Register.cshtml.cs calls SSO API correctly


    - Confirm RegisterAsync is called with form data
    - Verify redirect on success
    - _Requirements: 9.1, 9.2_
  - [x] 14.2 Verify error handling on Register page


    - Confirm duplicate email error displays
    - Confirm password policy violations display
    - Confirm terms acceptance is enforced
    - _Requirements: 9.3, 9.4, 9.5_
  - [x] 14.3 Verify parental consent handling


    - Confirm parental consent is displayed when required
    - Verify consent collection is enforced
    - _Requirements: 9.6_

- [x] 15. Audit Logout Data Binding






  - [x] 15.1 Verify Logout.cshtml.cs clears cookies and invalidates tokens

    - Confirm TGP_Token cookie is cleared
    - Confirm refresh token is invalidated via SSO API
    - Confirm redirect to login page
    - _Requirements: 10.1, 10.2, 10.3, 10.4_

- [x] 16. Audit MFA Verification Page Data Binding





  - [x] 16.1 Verify VerifyMfa.cshtml.cs calls SSO API correctly


    - Confirm VerifyMfaAsync is called with code
    - Verify token cookie is set on success
    - Verify error/retry displays on invalid code
    - _Requirements: 8.4, 8.5_

## Phase 10: Main Pages Data Binding Audit

- [x] 16. Audit Home Page (Index) Data Binding





  - [x] 16.1 Verify Index.cshtml.cs loads real data


    - Confirm device count from DeviceManagementService
    - Confirm alert count from AlertsService
    - Verify empty state shows onboarding guidance
    - _Requirements: 13.1, 13.2, 13.3, 13.4_
  - [x] 16.2 Verify verification banner displays for unverified users


    - Confirm banner shows when user is not verified
    - Verify appropriate messaging
    - _Requirements: 13.5_

- [x] 17. Audit Dashboard Page Data Binding






  - [x] 17.1 Verify Dashboard/Index.cshtml.cs loads real data

    - Confirm devices from DeviceManagementService
    - Confirm alerts from AlertsService
    - Verify device card navigation works
    - Verify empty state displays correctly
    - _Requirements: 14.1, 14.2, 14.3, 14.4, 14.5_

- [x] 18. Audit Timeline Page Data Binding






  - [x] 18.1 Verify Timeline/Index.cshtml.cs loads real data

    - Confirm events from TimelineService
    - Verify filters work correctly
    - Verify screenshot URLs use StorageService
    - _Requirements: 15.1, 15.2, 15.4_

- [x] 19. Checkpoint - Main pages verified





  - Ensure all tests pass, ask the user if questions arise.

## Phase 10: Device Management Pages Data Binding Audit

- [x] 20. Audit Devices Index Page Data Binding


  - [x] 20.1 Verify Devices/Index.cshtml.cs loads real data
    - Confirm devices from DeviceManagementService
    - Verify quick action buttons create commands
    - Verify command feedback displays
    - _Requirements: 16.1, 16.2, 16.3, 16.4_

- [x] 21. Audit Device Details Page Data Binding






  - [x] 21.1 Verify Devices/Details.cshtml.cs loads real data

    - Confirm device metadata from database
    - Confirm monitored users display
    - Confirm configuration from DeviceManagementService
    - Verify config save publishes to RabbitMQ
    - _Requirements: 17.1, 17.2, 17.3, 17.4, 17.5_

- [x] 22. Audit Device Commands Page Data Binding






  - [x] 22.1 Verify Devices/Commands.cshtml.cs loads real data

    - Confirm commands from database
    - Verify command creation via DeviceManagementService
    - Verify RabbitMQ publishing
    - _Requirements: 18.1, 18.2, 18.3, 18.5_

- [x] 23. Audit Detected Users Page Data Binding






  - [x] 23.1 Verify DetectedUsers page loads real data

    - Confirm detected users from database
    - Verify Monitor/Ignore actions work
    - Verify audit fields are updated
    - _Requirements: 19.1, 19.2, 19.3, 19.5_

- [x] 24. Audit Device User Settings Page Data Binding






  - [x] 24.1 Verify Devices/UserSettings.cshtml.cs loads real data

    - Confirm settings from database
    - Verify toggle changes save to database
    - Verify RabbitMQ publishing on save
    - _Requirements: 20.1, 20.2, 20.3, 20.4, 20.5_

## Phase 11: Children & Family Pages Data Binding Audit

- [x] 25. Audit Children Index Page Data Binding






  - [x] 25.1 Verify Children/Index.cshtml.cs loads real data

    - Confirm profiles from ChildrenService
    - Verify create form works
    - Verify empty state displays
    - _Requirements: 21.1, 21.2, 21.3, 21.4, 21.5_

- [x] 26. Audit Children Details Page Data Binding





  - [x] 26.1 Verify Children/Details.cshtml.cs loads real data

    - Confirm profile with linked accounts
    - Verify link/unlink actions work
    - Verify rename and delete work
    - _Requirements: 22.1, 22.2, 22.3, 22.4, 22.5_

- [x] 27. Audit Children Settings Page Data Binding






  - [x] 27.1 Verify Children/Settings.cshtml.cs loads real data

    - Confirm settings from database
    - Verify color/avatar changes save
    - _Requirements: 23.1, 23.2, 23.3, 23.4_

## Phase 12: Account & Tenant Pages Data Binding Audit

- [x] 28. Audit Account Profile Page Data Binding
  - [x] 28.1 Verify Account/Profile.cshtml.cs loads real data
    - Confirm user info from claims
    - Confirm MFA status from SSO API
    - Verify MFA enable/disable/verify flows
    - _Requirements: 24.1, 24.2, 24.3, 24.4, 24.5, 24.6_

- [x] 29. Audit Account Settings Page Data Binding






  - [x] 29.1 Verify Account/Settings.cshtml.cs loads real data

    - Confirm preferences from UserPreferencesService
    - Verify theme changes persist and apply
    - _Requirements: 25.1, 25.2, 25.3, 25.4, 25.5_



- [x] 30. Audit Tenant Index Page Data Binding




  - [x] 30.1 Verify Tenant/Index.cshtml.cs loads real data

    - Confirm members from TenantService
    - Verify member remove action works
    - _Requirements: 27.1, 27.2, 27.3, 27.4_

- [x] 31. Audit Tenant Invite Page Data Binding






  - [x] 31.1 Verify Tenant/Invite.cshtml.cs works correctly

    - Confirm invitation via TenantService
    - Verify existing user handling
    - Verify email invitation for new users
    - _Requirements: 28.1, 28.2, 28.3, 28.4_

- [x] 32. Checkpoint - Account and tenant pages verified





  - Ensure all tests pass, ask the user if questions arise.

## Phase 13: Billing Pages Data Binding Audit

- [x] 33. Audit Billing Index Page Data Binding









  - [x] 33.1 Verify Billing/Index.cshtml.cs loads real data


    - Confirm subscription from SSO API
    - Confirm plans from SSO API
    - Verify cancellation works
    - _Requirements: 29.1, 29.2, 29.3, 29.4_

- [x] 34. Audit Billing Checkout Page Data Binding






  - [x] 34.1 Verify Billing/Checkout.cshtml.cs works correctly

    - Confirm plan details display
    - Verify payment processing
    - Verify success/failure handling
    - _Requirements: 30.1, 30.2, 30.3, 30.4_

## Phase 14: Reporting Page Data Binding Audit

- [x] 35. Audit Reporting Page Data Binding






  - [x] 35.1 Verify Dashboard/Report.cshtml.cs loads real data

    - Confirm charts use Reporting API data
    - Verify filters work
    - Verify export generates real data
    - _Requirements: 31.1, 31.2, 31.3, 31.4_

## Phase 15: SignalR Real-Time Updates Audit

- [x] 36. Audit SignalR Connection and Updates



  - [x] 36.1 Verify SignalR connection on Dashboard page

    - Confirm TimelineHub connection established
    - Verify connection status indicator
    - _Requirements: 11.1, 11.9_

  - [x] 36.2 Verify real-time alert notifications

    - Confirm alerts push to clients
    - Verify notification banner displays with severity, device name, summary
    - Verify click navigation works
    - Verify dismiss removes banner and marks as seen
    - _Requirements: 11.2, 11.3, 12.1, 12.2, 12.3, 12.4_

  - [x] 36.3 Verify alert batching for rapid succession

    - Confirm multiple alerts are batched to avoid overwhelming user
    - _Requirements: 12.5_

  - [x] 36.4 Verify real-time device status updates

    - Confirm status changes push to clients
    - Verify device cards update without refresh
    - _Requirements: 11.4, 11.5_

  - [x] 36.5 Verify real-time command status updates

    - Confirm command status pushes to clients
    - Verify command UI updates
    - _Requirements: 11.6, 11.7_


  - [x] 36.6 Verify real-time timeline events
    - Confirm timeline events push to Timeline page
    - Verify events append without page refresh

    - _Requirements: 11.8, 15.3_

  - [x] 36.7 Verify SignalR reconnection behavior
    - Confirm auto-reconnect with exponential backoff
    - Verify missed events are fetched on reconnection

    - _Requirements: 11.10, 11.11_
  - [x] 36.8 Verify Redis backplane for multi-client support


    - Confirm SignalR uses Redis for message distribution
    - _Requirements: 11.12_

## Phase 16: Navigation and Form Validation Audit

- [x] 37. Audit Navigation and Layout Data Binding





  - [x] 37.1 Verify layout displays user info from claims


    - Confirm user name in header
    - Confirm sidebar highlights current page
    - _Requirements: 32.1, 32.2_
  - [x] 37.2 Verify theme toggle persists and applies


    - Confirm preference saves to UserPreferencesService
    - Confirm TGP_Theme cookie updates
    - _Requirements: 32.3_
  - [x] 37.3 Verify verification status gating


    - Confirm feature access is gated based on verification status
    - Verify appropriate UI displays for unverified users
    - _Requirements: 32.4_

- [x] 38. Audit Form Validation Across All Pages






  - [x] 38.1 Verify validation errors display correctly

    - Confirm field-level errors adjacent to fields
    - Confirm server errors preserve form state
    - _Requirements: 33.1, 33.2_

  - [x] 38.2 Verify success feedback displays correctly

    - Confirm confirmation messages show
    - Confirm navigation/refresh works
    - _Requirements: 33.3, 33.4_

  - [x] 38.3 Verify required field handling

    - Confirm submission prevented when required fields empty
    - Confirm required fields are indicated
    - _Requirements: 33.4_

- [x] 39. Final Checkpoint - All audits complete





  - Ensure all tests pass, ask the user if questions arise.
