#!/usr/bin/env pwsh

# Robust Database Initialization Script
# Usage: ./init-db.ps1
# Prerequisites: .NET SDK 10.0+

$ErrorActionPreference = "Stop"

Write-Host "Starting TGP Database Initialization..." -ForegroundColor Cyan

# 1. Build the Migrator Tool
Write-Host "Building TGP.DbMigrator..." -ForegroundColor Yellow
dotnet build TGP.DbMigrator/TGP.DbMigrator.csproj -c Release --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to build TGP.DbMigrator."
    exit 1
}

# 2. Configure Environment
if (-not $env:TGP_DB_CONNECTION) {
    # Default fallback for local development
    $defaultConn = "Host=localhost;Database=tgp_sso;Username=postgres;Password=postgres"
    Write-Warning "TGP_DB_CONNECTION environment variable is not set."
    Write-Host "Using default local connection string: $defaultConn" -ForegroundColor DarkGray
    $env:TGP_DB_CONNECTION = $defaultConn
}

# 3. Run Migrations
Write-Host "Executing Migrations..." -ForegroundColor Yellow
try {
    dotnet run --project TGP.DbMigrator/TGP.DbMigrator.csproj -c Release --no-build
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Database initialized successfully." -ForegroundColor Green
    } else {
        throw "Migrator exited with code $LASTEXITCODE"
    }
}
catch {
    Write-Error "❌ Migration failed: $_"
    exit 1
}
