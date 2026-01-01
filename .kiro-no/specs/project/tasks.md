# Project Tasks

## Phase 1: Foundation & Infrastructure (Current Focus)

**Goal**: Establish a stable, reproducible local development environment and core backend infrastructure capable of supporting multi-tenancy and high-volume real-time data.

- [x] **Project Organization**
    - [x] Unify `TGP.Data` (Single Source of Truth).
    - [x] Create root `TGP.sln`.
    - [x] Consolidate tools and scripts.
- [x] **Local Infrastructure (Docker)**
    - [x] Create `docker-compose.yml` for dependencies:
        - PostgreSQL (Data, Identity, Multi-tenant Schema)
        - Redis (Cache, SignalR Backplane, Rate Limiting)
        - RabbitMQ (Message Broker for async processing)
        - MinIO/LocalStack (S3 Emulation for screenshots/logs)
- [x] **Database Management**
    - [x] Automate migration application (`init-db` script).
    - [x] Implement multi-tenant data seeding.
- [x] **Testing & CI/CD**
    - [x] Fix `TGP.Microservices.DeviceGateway` integration tests.
    - [x] Establish CI pipeline (Build, Test, Docker Publish).

## Phase 2: Microservices Implementation

- [x] Update `TGP.Microservices.DeviceGateway` to support MessagePack protocol for SignalR.
- [x] Update `TGP.Data` with an `Alert` entity.
- [x] Implement content analysis engine in `TGP.Microservices.Analysis`.
- [x] Implement Chrome Extension Content Analysis (MutationObserver, Event Queue, Blocking).
- [x] Android Client: Add `EncryptedSharedPreferences` for secure token storage.
- [x] Android Client: Create installation guide (`INSTALL.md`) for Chromebooks.
- [x] Implement Device Registration for Chrome Extension and Android Client (Login -> Register flow).


## Backlog

- [ ] Create `TGP.Microservices.Administration` (Internal Admin API) to restore parent command/config functionality.
- [ ] Check for other missing documentation or setup scripts

## Documentation & Standards

- [x] Add `/docs` for API contracts, ERD, and sequence diagrams
- [x] Define per-feature Definition of Done in `tasks.md`

## User Dashboard (Full-Stack Microservice) — Implementation Tracker

### Architecture & Auth
- [x] Configure ASP.NET Core Authentication/Authorization
- [x] Map principal from `TGP_Token` and populate claims
- [x] Protect sensitive pages with `[Authorize]`
- [x] Implement refresh token handling
- [x] Configure Serilog and Global Error Handling

### UI/UX & Theming
- [x] Implement theme switcher (Light/Dark) with persistence
- [x] Implement theme auto mode detection (prefers-color-scheme)
- [ ] Implement per-user theme preferences (server-side)
- [x] Add user menu with Logout
- [x] Add footer with Privacy, Terms, and Contact links
- [x] Implement Cookie Consent banner
- [x] Update branding (Logo replacement)
- [x] Accessibility pass (keyboard, ARIA, focus)
- [ ] Performance pass (pagination, caching, lazy assets)

### Account
- [x] Login page (SSO-backed)
- [x] Register page
- [x] MFA verify flow
- [x] Logout page
- [ ] Profile improvements and Settings page

### Dashboard
- [x] Devices grid and recent alerts section
- [ ] Alerts severity mapping and dismiss/acknowledge
- [ ] Real-time alerts via SignalR

### Devices
- [x] Device list
- [x] Device details (name/type/last seen, users)
- [x] Detected Users management (view, monitor, ignore)
- [x] Commands (Lock/Sync/Diagnostics/Message) basic
- [ ] Command status tracking (ack/completion) with UI
- [ ] Unified configuration management with concurrency checks

### Device/Child/Login UX Redesign (Phases 3–9)
- [x] Add detected-user conversational review component partial
- [x] Add quick-assign dropdown component partial
- [x] Update dashboard alert to “new logins to review” flow
- [x] Add per-device login counts on Devices list
- [x] Redesign Device Details into categorized login sections
- [x] Fix login state classification (pending/unassigned/ignored)
- [x] Redesign Detected Users page (rename, flow, bulk actions)
- [x] Limit UserSettings when unassigned (basic monitoring only)
- [x] Add UserSettings child selector for linked logins
- [x] Rework UnassignedUsers into “Logins I’m Watching” manager
- [x] Add UnassignedUsers actions (edit rules, stop watching, bulk)
- [x] Add navigation link to UnassignedUsers in main menu
- [x] Update Children Details with “Manage rules” links (profileId)
- [ ] Add Children Details “Available to Connect” section
- [ ] Complete terminology cleanup (“login” vs “account”) across UI
- [ ] Add consistent loading/empty/error states across redesigned pages
- [ ] Wire real-time updates for new logins on redesigned surfaces
- [ ] Restore missing docs-inprog walkthrough.md or update doc links

### Reporting
- [x] Daily report page with charts
- [ ] Filters and export support
- [ ] Loading skeletons and empty states

### Children
- [x] Pages present (Index/Create/Details/Settings)
- [ ] Hook CRUD to services; validation and error states

### Tenant
- [x] Index and Invite pages present
- [ ] Invitations flow; membership management

### Billing
- [x] Pages present (Index/Checkout)
- [ ] Plans and subscription lifecycle; payment errors

## Definition of Done — User Dashboard

### Architecture & Auth
- Cookie/JWT authentication configured; sensitive routes protected with `[Authorize]`
- `TGP_Token` mapped to claims; refresh tokens handled in `HttpClient`
- Unauthenticated users redirected appropriately; verification gates enforced for advanced actions
- Auth errors logged with correlation IDs

### UI/UX & Theming
- Theme persistence stored server-side per user; auto mode honored
- Accessibility baseline: keyboard navigation, ARIA roles/landmarks, visible focus
- Performance baseline: pagination on lists, caching, lazy assets
- Footer includes Privacy, Terms, and Contact links on all pages

### Account
- Profile page displays identity and verification status
- Settings page exposes theme and account controls
- Error and redirect states verified across auth flows

### Dashboard
- Devices grid shows name/type/status/last seen with filters
- Recent alerts include severity mapping and acknowledge/dismiss actions
- Real-time updates via SignalR with backpressure strategy
- Loading, empty, and error states implemented

### Devices
- Device details include capability sections per platform via `CapabilityService`
- Remote commands wired with status tracking (ack/completion) and UI feedback
- Advanced actions gated by verification state
- Loading, error, and feedback banners present

### Reporting
- Daily/weekly charts implemented with filters (device/profile/date)
- Export to CSV/PDF available
- Empty/loading/error states implemented; data correctness validated

### Children
- CRUD wired to services with validation and error surfaces
- Link/unlink accounts flows implemented with confirmation states

### Tenant
- Members and roles displayed; role-based actions enforced
- Invitation flow implemented with status tracking and error handling

### Billing
- Plans and subscription lifecycle implemented; payment errors surfaced
- Receipts issued; renewal status shown

### Accessibility
- Keyboard navigation, ARIA, focus rings, and color contrast validated
- Reduce motion preferences respected

### Performance
- Pagination on lists; lazy thumbnails; caching of aggregates
- Minified assets, deferred scripts, optimized fonts; metrics validated
