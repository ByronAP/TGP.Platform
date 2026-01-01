# TGP Project Master Plan

## Executive Summary
This document outlines the comprehensive strategic roadmap for TGP (The Good Parent), a cross-platform, multi-tenant SaaS parental control system. The goal is to deliver a privacy-first, secure, and scalable solution that empowers parents to guide their children's digital journey across all devices.

## Phase 1: Foundation & Infrastructure (Completed)
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

## Phase 2: Core Service & Web Development (Completed)
**Goal**: Build and refine the robust microservices architecture and web platforms required to support millions of devices and users.

- [x] **TGP.Microservices.SSO (Identity & Access)** *[Completed]*
    - [x] Basic JWT Authentication.
    - [x] User Registration.
    - [x] Enhance Multi-Factor Authentication (MFA).
    - [x] Implement Parental Consent Flow (COPPA Compliance).
    - [x] Subscription/Billing Integration (Stripe/Paddle).
- [x] **TGP.Microservices.DeviceGateway (IoT Core)** *[Completed]*
    - [x] Basic Heartbeat processing.
    - [x] Configuration distribution.
    - [x] Protocol optimization for low-bandwidth environments.
    - [x] Real-time command routing (SignalR).
    - [x] Batch processing pipeline for telemetry data.
- [x] **TGP.Microservices.Analysis (AI/Safety)** *[Completed]*
    - [x] Content analysis engine (Text & Image classification using META API).
    - [x] Anomaly detection for "concerning behavior" alerts.
    - [x] Data retention policy enforcement (Tier-based).
- [x] **TGP.Microservices.Reporting** *[Completed]*
    - [x] Aggregated activity reports.
    - [x] Screen time calculation engine.
- [x] **TGP.UserDashboard (Web)** *[Completed]*
    - [x] Basic project structure (ASP.NET Core MVC/Razor).
    - [x] Real-time activity feed.
    - [x] Rule configuration (Time limits, App blocking).
    - [x] Subscription management.
    - [x] Alert center.

## Phase 3: Client Ecosystem (Cross-Platform) (Current Focus)
**Goal**: Deliver native agents for all promised platforms, ensuring feature parity where OS constraints allow.

- [x] **Windows Client (WPF/.NET)** *[Completed]*
    - [x] App usage tracking & blocking.
    - [x] Web filtering (WFP/LSP driver).
    - [x] Screenshot capture (Privacy-aware).
    - [x] Offline enforcement of time limits.
- [ ] **macOS Client (Swift/SwiftUI)** *[Planned]*
    - [ ] System Extension for Network Filtering.
    - [ ] Screen Time API integration.
- [ ] **Mobile Clients**
    - [ ] **Android (Kotlin/Kotlin Multiplatform)** *[Planned]*: Accessibility Service usage for monitoring, VPN Service for filtering.
    - [ ] **iOS (Swift)** *[Planned]*: Screen Time API, MDM/Network Extension integration.
- [x] **ChromeOS Extension** *[Completed]*
    - [x] Browser-level monitoring and filtering.
    - [x] Content analysis & Batch reporting.
    - [x] Companion features (Time warnings, Nags/Notifications).
- [x] **TGP Companion (Android App)** *[Completed - Prototype]*
    - [x] "Annoying & Persistent" Overlay Service (SYSTEM_ALERT_WINDOW).
    - [x] Independent Time Limit enforcement.
    - [x] SSO Integration.
- [ ] **Linux Agent** *[Planned]*
    - [ ] Daemon for usage tracking and IPTables management.

## Phase 4: Marketing & Mobile Experience
**Goal**: Provide intuitive interfaces for parents to manage their family's digital life on mobile and attract new users.

- [ ] **TGP.LandingPage (Marketing)** *[Started]*
    - [x] Core layout and content.
    - [x] Waitlist functionality (Cloudflare Workers).
    - [ ] SEO optimization.
    - [ ] Blog/Resources section.
- [ ] **Parent Companion App (Mobile)** *[Planned]*
    - [ ] Push notifications for requests/alerts.
    - [ ] Quick actions (Pause internet, Bonus time).

## Phase 5: Network-Wide Protection (Future Expansion)
**Goal**: Secure devices that cannot run native agents (Consoles, VR, IoT).

- [ ] **TGP Virtual Shield** *[Planned]*
    - [ ] DNS-based filtering service.
    - [ ] VPN-based traffic inspection.
- [ ] **TGP Hardware Gateway** *[Planned]*
    - [ ] Firmware for OpenWRT/Raspberry Pi.
    - [ ] Device fingerprinting and profile assignment.

## Phase 6: Production Readiness & Compliance
**Goal**: Ensure the system is secure, compliant, and performant before public launch.

- [ ] **Security & Privacy**
    - [ ] Third-party security audit / Pen-testing.
    - [ ] End-to-end encryption verification.
    - [ ] COPPA/GDPR/CCPA compliance review.
- [ ] **Infrastructure Scaling**
    - [ ] Kubernetes (K8s) deployment manifests (Helm charts).
    - [ ] Auto-scaling policies for Gateway and Analysis services.
    - [ ] Global CDN setup for static assets.
    - [ ] Complaints & Compliance Request System (GDPR/COPPA/Disputes).
- [ ] **Admin Portal (Web)**
    - [ ] Customer support management.
    - [ ] Tenant/User management.
    - [ ] System health monitoring and alerting.
    - [ ] System management.
    - [ ] Compliance & Dispute Resolution Center.
- [ ] **Support & Operations**
    - [ ] Automated system health monitoring and alerting.
