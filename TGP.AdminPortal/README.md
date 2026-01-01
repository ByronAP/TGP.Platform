# TGP Admin Portal

Admin portal for system operators to control and monitor the TGP parental control platform.

## Features

- **Dashboard** - Real-time KPIs (tenants, subscriptions, revenue, devices)
- **Plan Management** - Create, edit, and manage subscription plans
- **Subscription Management** - View and modify customer subscriptions
- **Billing Analytics** - Revenue by plan, subscriber counts

## Prerequisites

- .NET 10 SDK
- SQL Server (Azure SQL or local)
- Redis (optional, for caching)

## Running Locally

```bash
dotnet run --project src/TGP.AdminPortal
```

Default URL: `https://localhost:5001`

## Authentication

Requires `Admin` or `SystemAdmin` role to access. Login via SSO service.

### Creating an Admin User

1. Run the DbMigrator to seed the SystemAdmin role:
   ```bash
   dotnet run --project ../TGP.DbMigrator/src/TGP.DbMigrator -- --seed
   ```

2. Assign the `SystemAdmin` role to a user via SQL:
   ```sql
   INSERT INTO tgp.AspNetUserRoles (UserId, RoleId)
   VALUES ('your-user-id', '00000000-0000-0000-0000-000000000003');
   ```

## Configuration

| Setting | Description |
|---------|-------------|
| `ConnectionStrings:DefaultConnection` | SQL Server connection string |
| `ServiceUrls:Sso` | SSO service URL |
| `Redis:ConnectionString` | Redis for session caching |
| `Stripe:SecretKey` | Stripe API key (optional) |

## Project Structure

```
TGP.AdminPortal/
├── src/TGP.AdminPortal/
│   ├── Pages/
│   │   ├── Account/     # Login, Logout
│   │   ├── Dashboard/   # KPIs and overview
│   │   ├── Plans/       # Plan CRUD
│   │   ├── Subscriptions/ # Subscription management
│   │   └── Shared/      # Layout, partials
│   ├── wwwroot/css/     # Admin styles
│   └── Program.cs       # Entry point
└── TGP.AdminPortal.sln
```

## Deployment

Deploy as an Azure App Service using the same resource group as other TGP services.
