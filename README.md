# TGP (The Good Parent)

TGP is a cross-platform, multi-tenant SaaS parental control system designed to be privacy-first, secure, and scalable.

## 📂 Project Structure

### Microservices
| Project | Description |
|---------|-------------|
| [TGP.Microservices.SSO](TGP.Microservices.SSO/) | Identity, Authentication, and MFA service |
| [TGP.Microservices.DeviceGateway](TGP.Microservices.DeviceGateway/) | IoT gateway for device communication |
| [TGP.Microservices.Analysis](TGP.Microservices.Analysis/) | Anomaly detection and data processing |
| [TGP.Microservices.Reporting](TGP.Microservices.Reporting/) | Reports and analytics generation |

### Client Applications
| Project | Description |
|---------|-------------|
| [TGP.Windows.Client](TGP.Windows.Client/) | Native Windows monitoring agent |
| [TGP.Client.Android](TGP.Client.Android/) | Android enforcement app |
| [TGP.ChromeExtension](TGP.ChromeExtension/) | Chrome/ChromeOS web filtering extension |

### Web Applications
| Project | Description |
|---------|-------------|
| [TGP.UserDashboard](TGP.UserDashboard/) | Parent web portal for device management |
| [TGP.LandingPage](TGP.LandingPage/) | Public marketing website |

### Shared & Tools
| Project | Description |
|---------|-------------|
| [TGP.Data](TGP.Data/) | Shared EF Core data layer |
| [TGP.DbMigrator](TGP.DbMigrator/) | Database migration CLI tool |
| [TGP.TestDataGenerator](TGP.TestDataGenerator/) | Test data generation utility |
| [TGP.Assets](TGP.Assets/) | Brand assets and logos |

## 🚀 Getting Started

### Prerequisites
- .NET 10 SDK
- Docker Desktop

### Quick Setup
We provide a unified initialization script to start infrastructure (Postgres, Redis, RabbitMQ) and seed the database.

```powershell
# Run from root directory
./init-db.ps1
```

### Running Services
Each service can be run independently:

```bash
# SSO Service (Auth)
dotnet run --project TGP.Microservices.SSO/src/TGP.Microservices.SSO

# Device Gateway
dotnet run --project TGP.Microservices.DeviceGateway/src/TGP.Microservices.DeviceGateway

# User Dashboard
dotnet run --project TGP.UserDashboard/src/TGP.UserDashboard
```

## 📖 Documentation

- **Setup Flow**: See [docs/SETUP_FLOW.md](docs/SETUP_FLOW.md)
- **ERD (Data Model)**: See [docs/ERD.md](docs/ERD.md)
- **Sequence Diagrams**: See [docs/sequences.md](docs/sequences.md)
- **API Docs**:
  - [SSO API Usage](TGP.Microservices.SSO/docs/API_USAGE.md)
  - [Device Gateway API Guide](TGP.Microservices.DeviceGateway/docs/gateway-api-guide.md)

## 🛠 Scripts

- **Database Init**: `init-db.ps1` - Migrates and seeds the DB using the `TGP.DbMigrator` tool.
- **Client CI**: `TGP.Windows.Client/scripts/` - CI/CD scripts for the Windows agent.
