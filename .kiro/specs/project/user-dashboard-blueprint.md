# TGP User Dashboard Blueprint

## Overview
Goal: Deliver a production‑grade, beautiful, and fully functional ASP.NET Core Razor Pages microservice that handles both UI and backend logic for end users. This blueprint enumerates features, pages, controls, styles, flows, integrations, and current status, providing an auditable plan.

## Feature Matrix
- [x] Theme Switching: Light/Dark toggle with persistence (localStorage), CSS variables
- [ ] Theme Preferences: Per‑user server persistence; sync on login
- [x] Logout: Clears session and returns to login
- [x] Login: Email/password; sets access token cookie used for outbound API calls
- [x] Register: Email/password; Terms acceptance
- [x] MFA Challenge & Verify: Flow implemented via SSO API
- [ ] Authorization: Protect pages with [Authorize]; configure AddAuthentication and principal population from token
- [ ] Refresh Tokens: Client/server token rotation and auto‑renewal
- [x] Devices List: Cards with status and types
- [x] Device Details: Real data (name, type, last seen), users on device
- [x] Device Commands: Create command and publish event; basic UI actions
- [ ] Command Status: Acknowledgement/completion tracking and UI feedback
- [x] Dashboard Alerts: Recent alerts via EF for current user
- [ ] Alerts UX: Severity mapping, dismiss/acknowledge, real‑time updates (SignalR)
- [x] Reporting (Daily): Bar/Pie charts on Report page using Chart.js
- [ ] Reporting UX: Export, filters, pagination, empty states
- [x] Children: Index/Create/Details/Settings pages present
- [x] Children UX: Cross‑device identity mapping via MonitoredProfiles/Links; real data
- [ ] Children UX: Validation, unlink actions, color/avatar customization
- [x] Tenant: Index/Invite pages present
- [ ] Tenant UX: Invitations flow and membership management complete
- [x] Billing: Index/Checkout pages present
- [ ] Billing Integration: Plans, subscription status, payments, errors
- [ ] Accessibility: Keyboard navigation, ARIA roles, focus states
- [ ] Performance: Caching, pagination, lazy loading where appropriate

## Pages & Routes
- Account
  - /Account/Login
  - /Account/Register
  - /Account/Profile
  - /Account/VerifyMfa
  - /Account/Logout
  - [ ] /Account/Settings (missing page; remove link or implement)
- Dashboard
  - /Dashboard/Index
  - /Dashboard/Report
- Devices
  - /Devices/Index
  - /Devices/Details/{id}
  - /Devices/UserSettings/{deviceId}/{username}
  - /Devices/Commands
- Children
  - /Children/Index
  - /Children/Create
  - /Children/Details/{id}
  - /Children/Settings/{id}
- Tenant
  - /Tenant/Index
  - /Tenant/Invite
- Billing
  - /Billing/Index
  - /Billing/Checkout
- Misc
  - /
  - /Privacy
  - /Error

## Controls & Components
- Navigation: Sidebar with sections (Dashboard, Family, Devices, Account)
- Header: Desktop header with Theme Toggle and User Menu (Profile, Settings, Logout)
- Cards: Standard glass card with hover elevation
- Buttons: Primary/Secondary variants; small sizes
- Badges: Online/Offline/Warning
- Toggles: Capability switches with checked/on styles
- Forms: Form control design and focus states
- Empty States: Responsive empty tiles with icon and messaging

## Styles & Design System
- Design tokens: Brand colors, neutrals, shadows, spacing, radius, transitions
- Light/Dark theming: CSS variable overrides under `body.light-mode`
- Glassmorphism: Glass backgrounds and borders for cards/layout sections
- Responsiveness: Mobile sidebar overlay; desktop sticky header; grid layouts
- Accessibility: Contrast and focus state enhancements [pending]

## Authentication & Authorization Flow
1. Login: User submits email/password; SSO returns token(s)
2. Cookie: Access token stored as `TGP_Token` (HttpOnly, Secure)
3. Outbound Auth: `AuthHeaderHandler` attaches Bearer for API calls
4. Authorization: [Pending] Configure AddAuthentication/AddAuthorization; map principal from TGP_Token; protect pages with [Authorize]; populate claims (NameIdentifier, is_verified)
5. MFA: Challenge/verify endpoints; redirect on success
6. Logout: Clears cookies/sessions; redirect to login

## Device Management Flow
1. List devices for user (SSO DevicesController)
2. Device details: Show metadata and last seen; users on device
3. Commands: Create `DeviceCommand` → publish `command.created` via RabbitMQ
4. Config: [Decision required] Unify path via internal DB service or SSO/Gateway Admin APIs; handle version concurrency (ETag) and audit
5. Status: [Pending] Show command acknowledgement/completion with latencies

## Alerts Flow
1. Read recent alerts via EF for devices owned by current user
2. Severity mapping: Based on source/keyword [pending]
3. UX: Dismiss/acknowledge; filter by severity/time; pagination [pending]
4. Real‑time: SignalR subscription for new alerts [pending]

## Reporting Flow
1. Fetch daily report per device via Reporting API
2. Weekly chart: Aggregate last 7 days; Bar chart
3. Top apps chart: Pie data aggregated across week
4. Enhancements: Filters, export CSV/PDF, loading skeletons [pending]

## API & Services Integration
- ITgpApiClient: Auth, devices, reporting, subscription
- AlertsService (EF/TgpDbContext): Recent alerts by user devices
- DeviceManagementService (EF + RabbitMQ): Config update/versioning; command creation and event publishing
- RabbitMQ Exchanges:
  - `tgp.config` → `config.updated`
  - `tgp.devices` → `command.created`

## Data Model Mapping (TGP.Data)
- Device, DeviceConfiguration, DeviceCommand, Alert, MonitoredUser (+ Settings)
- Relationships: Device → Configuration, Commands, Alerts, MonitoredUsers

## Accessibility
- [ ] Keyboard navigation across menus, toggles, and forms
- [ ] Focus indicators and skip‑to‑content
- [ ] ARIA roles for components and landmarks

## Performance
- [ ] Pagination on lists (Devices, Alerts, Children)
- [ ] Lazy load assets; defer charts; cache computed aggregates
- [ ] Minify CSS/JS; preconnect and font optimizations

## Open Gaps (Prioritized)
1. Authorization scaffolding and protected routes
2. /Account/Settings page (implement or remove link)
3. Unified device config path with concurrency checks and audit
4. Command status tracking and UI feedback loop
5. Alerts severity, dismissal, and real‑time updates
6. Children/Tenant/Billing flows finalized with validation and error handling
7. Accessibility and performance enhancements

## Execution Plan (High‑Level)
- Backend (backend‑architect):
  - Add authentication configuration; claims population; [Authorize] on sensitive pages
  - Consolidate device config path and implement ETag/version checks
  - Add command status tracking endpoints or DB queries; publish/subscribe flow
- Frontend (frontend‑architect):
  - Wire protected pages; add auth guards; error handling and loading states
  - Implement alerts UX (severity display, dismiss/acknowledge)
  - Add pagination and filters; improve empty states
- UI/UX (ui‑designer):
  - Fine‑tune tokens and components; accessibility pass; responsive polish
  - Create consistent component library patterns for buttons/cards/badges/toggles/forms

## Traceability
This blueprint is the authoritative plan for `TGP.UserDashboard`. All work items must be reflected in `.kiro/specs/project/tasks.md` and updated as completed.
