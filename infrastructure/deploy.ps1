param (
    [string]$ResourceGroupName = "rg-tgp-prod-northcentralus",
    [string]$Location = "northcentralus",
    [string]$Environment = "prod",
    [securestring]$DbPassword
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

# Deploy Infrastructure
Write-Host "Deploying Bicep Template..." -ForegroundColor Yellow
az deployment group create `
    --resource-group $ResourceGroupName `
    --template-file ./main.bicep `
    --parameters environmentName=$Environment dbPassword=$DbPasswordPlain

if ($LASTEXITCODE -eq 0) {
    Write-Host "Deployment Completed Successfully!" -ForegroundColor Green
} else {
    Write-Error "Deployment Failed."
}
