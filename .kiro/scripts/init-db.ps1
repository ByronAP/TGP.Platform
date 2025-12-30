Write-Host "Starting TGP Database Initialization..."

# Check Docker
if (!(Get-Command docker-compose -ErrorAction SilentlyContinue)) {
    Write-Error "docker-compose not found."
    exit 1
}

# Start Infrastructure
Write-Host "Starting infrastructure containers..."
docker-compose up -d
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Wait for Postgres
Write-Host "Waiting for Postgres to be ready..."
Start-Sleep -Seconds 5

# Set Connection String Env Var for TGP.Data and TestDataGenerator
# Note: TgpDbContextFactory looks for TGP_DB_CONNECTION
# TestDataGenerator looks for ConnectionStrings:DefaultConnection
$connStr = "Host=localhost;Database=tgp_db;Username=tgp_user;Password=tgp_password"
$env:TGP_DB_CONNECTION = $connStr
$env:ConnectionStrings__DefaultConnection = $connStr

# Apply Migrations
Write-Host "Applying EF Core Migrations..."
# We run 'dotnet ef database update' against TGP.Data
# We need to ensure we are in the root directory
$root = Get-Location
Write-Host "Root: $root"

dotnet ef database update --project TGP.Data/TGP.Data.csproj --startup-project TGP.Data/TGP.Data.csproj --verbose
if ($LASTEXITCODE -ne 0) { 
    Write-Error "Migration failed. Ensure dotnet-ef is installed (dotnet tool install --global dotnet-ef) and Postgres is running."
    exit $LASTEXITCODE 
}

# Seed Data
Write-Host "Seeding Data..."

# Build TestDataGenerator
dotnet build TGP.TestDataGenerator/TGP.TestDataGenerator.csproj -c Release
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Run Seeder
# Arguments: 1 user, 2 devices, --seed-only, --db-only, --no-clean
# We use --no-clean because we just migrated and migration might have added some initial data (though SystemCleaner handles it properly, skipping it is faster if we trust migrations)
# Actually, let's use --no-clean to preserve migration data just in case, relying on DataSeeder to add what's missing.
$seedArgs = @("1", "2", "--seed-only", "--db-only", "--no-clean")
$seedArgsStr = $seedArgs -join " "

# Run from project directory to find appsettings.json
Push-Location TGP.TestDataGenerator
try {
    dotnet run -c Release -- $seedArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Seeding failed."
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}

Write-Host "Initialization Complete."
