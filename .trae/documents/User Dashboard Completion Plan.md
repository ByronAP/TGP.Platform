## Phase 0 • Foundations

* [x] Create/validate blueprint [.kiro/specs/project/user-dashboard-blueprint.md]

* [x] Create/validate tracker [.kiro/specs/project/tasks.md]

* [x] Add /docs for API contracts, ERD, sequence diagrams

* [x] Define per-feature Definition of Done in tasks.md

## Phase 1 • Global Shell (Header/Sidebar/Footer/Theme)

* [x] Sidebar and desktop/mobile headers present

* [x] Theme toggle (Auto/Light/Dark) with persistence and system detection

* [x] Footer with Privacy (/Privacy), Terms (/Terms), Contact links on all pages

* [x] Unauth redirect rules; verification banner gating advanced actions

## Phase 2 • Authentication & Authorization

* [x] Configure cookie/JWT auth; map TGP_Token → claims

* [x] Protect routes with [Authorize]; redirect unauthenticated

* [x] HttpClient token refresh/error handling

* [x] Verify claims in UI (user menu, gating)

## Phase 3 • Home ("/")

* [x] Intro + CTAs (Add Device, Create Child)

* [x] Summary tiles (devices count, alerts today)

* [x] Onboarding card when devices=0

* [x] Loading/empty/error states

## Phase 4 • Dashboard ("/Dashboard/Index")

* [x] Device grid (name/type/status/last seen)

* [x] Recent Alerts list from EF

* [x] Filters (type/status/last-seen)

* [x] Verification banner logic

* [x] Loading/empty/error states

## Phase 5 • Timeline ("/Timeline")

* [x] Aggregate Alerts/Heartbeats/Batches/Commands/Detected Users

* [x] Filters (type/device/date range) & keyword search

* [x] Group by day; newest-first

* [x] Screenshot thumbnails (signed URLs)

* [x] SignalR real-time updates with backpressure

* [x] Loading/empty/link-error/hub reconnect states

## Phase 6 • Devices Index ("/Devices/Index")

* [x] Cards with quick actions (Lock/Sync)

* [x] Onboarding when empty

* [x] Loading/empty/command feedback

## Phase 7 • Device Details ("/Devices/Details/{id}")

* [x] Header (name/type/last seen; status badge)

* [x] Users list (MonitoredUsers) with Settings links

* [x] Remote Commands wired to /Devices/Commands

* [x] Capability sections per platform using CapabilityService

* [x] Advanced action gating by verification

* [x] Loading/empty/error; feedback banners

## Phase 8 • Detected Users ("/Devices/DetectedUsers")

* [x] Table (Device, Username, Session, DetectedAt, Status)

* [x] Actions: Monitor, Ignore

* [x] Action: Link to Profile (choose child profile)

* [x] Audit fields: ReviewedBy/ReviewedAt set

* [x] Banners: success/error

## Phase 9 • Children Index ("/Children/Index")

* [x] List real profiles (MonitoredProfiles)

* [x] Create profile form

* [x] Loading/empty/validation states

## Phase 10 • Children Details ("/Children/Details/{id}")

* [x] Header with DisplayName/avatar initial; link count

* [x] Linked Accounts list (DeviceName + Username)

* [x] Actions: link/unlink accounts

* [x] Profile management: rename/delete; avatar/color customization

* [x] Loading/empty/confirmation states

## Phase 11 • Account Pages

* [x] Login/Register/VerifyMfa/Settings/Logout present

* [x] Theme persistence via UserPreferencesService

* [x] Profile page shows identity and verification status

* [x] Error/redirect states verified

## Phase 12 • Reporting

* [x] Weekly/daily charts; filters (device/profile/date)

* [x] Export CSV/PDF

* [x] Empty/loading/error states

## Phase 13 • Billing

* [x] Index: plan/status/renewal/cancel; link to Checkout

* [x] Checkout: purchase flow; error handling; receipts

* [x] Mock payment integration for demo

## Phase 14 • Tenant

* [x] Index: members/roles; role-based actions

* [x] Invite: email invites; status tracking

* [x] Create/Link Tenant on demand

## Phase 15 • Branding

* [x] Replace raster logo with SVG; align colors/typography/spacing/radius tokens

* [x] Apply brand consistently to [_Layout] and [_AuthLayout]

## Phase 16 • Accessibility

* [x] Keyboard nav; skip links; focus rings

* [x] ARIA labels for icons/inputs

* [x] Color contrast audit

## Phase 17 • Performance

* [x] Minification/Bundling (default in .NET Core, verify)

* [x] Cache-Control headers for static assets

## Phase 18 • Observability

* [x] Structured logging (Serilog or default)

* [x] Global error handler / Error page polish

## Phase 19 • Legal & Consent

* [x] Footer legal links on all pages (Privacy/Terms/Contact)

* [x] Cookie consent banner

* [x] Privacy Policy / Terms pages linked

* [x] TermsAccepted + timestamp on Register; parental consent where applicable

* [x] Retention disclosure; soft delete behavior noted

## Definition of Done (Global)

* No demo/template data; all UI bound to real services

* Each checklist item implemented and verified; tests where applicable

* Docs updated: blueprint/tasks/API/ERD/sequences; legal links verified

## Traceability

* Blueprint: [.kiro/specs/project/user-dashboard-blueprint.md]

* Tasks: [.kiro/specs/project/tasks.md]

* Key files: Layout [/_Layout.cshtml], Auth Layout [/_AuthLayout.cshtml], CSS [site.css], JS [site.js], Services (DeviceManagement, Alerts, Timeline, Children, Preferences)
