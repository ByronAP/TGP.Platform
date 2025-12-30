param (
    [string]$ResourceGroupName = "rg-tgp-prod-northcentralus",
    [string]$Location = "northcentralus",
    [string]$Environment = "prod",
    [securestring]$DbPassword,
    [securestring]$JwtSecretKey
)

Write-Host "Starting TGP Infrastructure Deployment..." -ForegroundColor Cyan
Write-Host "Resource Group: $ResourceGroupName"
Write-Host "Location: $Location"
Write-Host "Environment: $Environment"

# Check for Azure CLI login
$azStatus = az account show 2>$null
if ($LASTEXITCODE -ne 0) {
    Write-Error "Not logged into Azure. Please run 'az login' first."
    exit 1
}

# Create Resource Group
Write-Host "Creating/Updating Resource Group..." -ForegroundColor Yellow
az group create --name $ResourceGroupName --location $Location

# Prompt for DB Password if not provided
if (-not $DbPassword) {
    $DbPassword = Read-Host "Please enter a secure password for the SQL Server" -AsSecureString
}
$DbPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($DbPassword))

# Prompt for JWT Secret Key if not provided
if (-not $JwtSecretKey) {
    $JwtSecretKey = Read-Host "Please enter a JWT secret key (min 32 characters)" -AsSecureString
}
$JwtSecretKeyPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($JwtSecretKey))

# Validate JWT key length
if ($JwtSecretKeyPlain.Length -lt 32) {
    Write-Error "JWT secret key must be at least 32 characters for HS256 security."
    exit 1
}

# Deploy Infrastructure
Write-Host "Deploying Bicep Template (includes Key Vault)..." -ForegroundColor Yellow
az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file ./main.bicep `
    --parameters environmentName=$Environment dbPassword=$DbPasswordPlain jwtSecretKey=$JwtSecretKeyPlain

if ($LASTEXITCODE -eq 0) {
    Write-Host "Deployment Completed Successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Key Vault has been created with all secrets." -ForegroundColor Cyan
    Write-Host "Services will automatically load secrets from Key Vault via KeyVault__Uri environment variable." -ForegroundColor Cyan
} else {
    Write-Error "Deployment Failed."
}
