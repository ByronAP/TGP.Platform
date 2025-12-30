# Project Architecture

## Overview
The project is a distributed system comprising multiple microservices and a shared data layer, built primarily with .NET 10 (ASP.NET Core).

## Components

### 1. TGP.Data (Shared Library)
- **Role**: Shared Data Layer (EF Core Entities, Repositories).
- **Tech**: .NET Class Library, EF Core.
- **Database**: PostgreSQL (Entities include Users, Devices, AuditLogs, etc.).
- **Note**: Currently appears to be duplicated/referenced in `TGP.Microservices.DeviceGateway/src/TGP.Data`.

### 2. TGP.Microservices.DeviceGateway
- **Role**: Manages communication with Windows Clients.
- **Features**: 
  - Device Authentication (JWT)
  - Heartbeat System
  - Configuration Management
  - Command & Control
  - Batch Uploads (Compressed)
  - SignalR Hub
- **Tech Stack**:
  - .NET 10
  - PostgreSQL (Neon Cloud)
  - Redis (StackExchange.Redis)
  - RabbitMQ
  - S3 Compatible Storage (Backblaze B2)
  - Compression: LZ4 & ZStandard

### 3. TGP.Microservices.SSO
- **Role**: Single Sign-On and Identity Management.
- **Features**:
  - User Registration & Login
  - JWT Token Management (Access/Refresh)
  - MFA (TOTP, SMS, Backup codes)
  - Device Management (Register/Revoke)
  - User Profile Management
- **Tech Stack**:
  - .NET 10
  - PostgreSQL
  - Redis (likely for token storage/blacklisting, inferred from usage)

### 4. TGP.LandingPage
- **Role**: Public facing landing page.
- **Tech**: Static HTML/CSS, Cloudflare Workers (`wrangler.toml`).

### 5. TGP.Assets
- **Role**: Static assets (images, logos).

## Infrastructure & Dependencies
- **Database**: PostgreSQL
- **Message Broker**: RabbitMQ
- **Cache**: Redis
- **Storage**: S3 Compatible (Backblaze B2)
- **Runtime**: Docker (supported via Dockerfiles in microservices)
