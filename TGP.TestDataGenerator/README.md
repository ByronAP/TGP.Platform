# TGP Test Data Generator

## Overview
**TGP.TestDataGenerator** is a utility tool for generating test data and managing test environments. It can seed databases with sample data and clean up test resources.

## Features
- **Data Seeding**: Generate realistic test users, devices, and activity data.
- **S3 Cleanup**: Remove test files from S3-compatible storage.
- **Database Reset**: Clear and reseed test databases.

## Tech Stack
- **Framework**: .NET 10 Console App
- **Storage**: AWS S3 SDK (Backblaze B2)
- **Database**: PostgreSQL via EF Core

## Usage
```bash
dotnet run --project TGP.TestDataGenerator
```

## Configuration
Configure `appsettings.json` with:
- Database connection string
- S3 endpoint and credentials
