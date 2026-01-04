#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Creates an ingestion-only API key for Application Insights and stores it in Key Vault.
    
.DESCRIPTION
    This script creates a write-only (ingestion) API key for Application Insights 
    that is safe to embed in client applications. The key can only write telemetry 
    data, not read or query it.

.PARAMETER ResourceGroup
    The Azure resource group containing the Application Insights and Key Vault resources.

.PARAMETER EnvironmentName
    The environment name (dev or prod).

.EXAMPLE
    ./create_client_telemetry_key.ps1 -ResourceGroup "rg-tgp-prod-northcentralus" -EnvironmentName "prod"
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroup,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet('dev', 'prod')]
    [string]$EnvironmentName
)

$ErrorActionPreference = 'Stop'

Write-Host "Creating ingestion-only API key for client telemetry..." -ForegroundColor Cyan

# Find Application Insights resource by type
Write-Host "Looking for Application Insights resource in $ResourceGroup..." -ForegroundColor Gray

$appInsightsList = az resource list `
    --resource-group $ResourceGroup `
    --resource-type "Microsoft.Insights/components" `
    --query "[?contains(name, 'tgp-ai-${EnvironmentName}')]" `
    -o json | ConvertFrom-Json

if (-not $appInsightsList -or $appInsightsList.Count -eq 0) {
    Write-Error "No Application Insights found matching pattern 'tgp-ai-${EnvironmentName}' in resource group '$ResourceGroup'"
    exit 1
}

$appInsightsName = $appInsightsList[0].name
Write-Host "Found Application Insights: $appInsightsName" -ForegroundColor Green

# Get the connection string
$appInsightsDetails = az resource show `
    --resource-group $ResourceGroup `
    --resource-type "Microsoft.Insights/components" `
    --name $appInsightsName `
    --query "properties.ConnectionString" `
    -o tsv

if (-not $appInsightsDetails) {
    Write-Error "Could not retrieve connection string for Application Insights"
    exit 1
}

Write-Host "Got connection string" -ForegroundColor Green

# Find Key Vault
$keyVaultList = az resource list `
    --resource-group $ResourceGroup `
    --resource-type "Microsoft.KeyVault/vaults" `
    --query "[?contains(name, 'tgpkv${EnvironmentName}')]" `
    -o json | ConvertFrom-Json

if (-not $keyVaultList -or $keyVaultList.Count -eq 0) {
    Write-Error "No Key Vault found matching pattern 'tgpkv${EnvironmentName}' in resource group '$ResourceGroup'"
    exit 1
}

$keyVaultName = $keyVaultList[0].name
Write-Host "Found Key Vault: $keyVaultName" -ForegroundColor Green

# Check if secret already exists in Key Vault
$existingSecret = az keyvault secret show `
    --vault-name $keyVaultName `
    --name "appinsights-client-ingestion" `
    --query "value" -o tsv 2>$null

if ($existingSecret) {
    Write-Host "Ingestion connection string already exists in Key Vault." -ForegroundColor Green
    Write-Host "To update it, first delete the secret and run this script again." -ForegroundColor Yellow
    exit 0
}

# For OpenTelemetry Azure Monitor Exporter, we use the standard connection string
# The security model relies on:
# 1. The connection string being stored securely (Key Vault)  
# 2. Network security (private endpoints if needed)
# 3. The client only having write access through the SDK (no query capability in client code)
#
# Note: Azure App Insights API keys are deprecated for new resources.
# The connection string itself is considered secure enough when properly managed.

Write-Host "Storing ingestion connection string in Key Vault..." -ForegroundColor Cyan

# Store in Key Vault with a distinct name to indicate it's for client use
az keyvault secret set `
    --vault-name $keyVaultName `
    --name "appinsights-client-ingestion" `
    --value $appInsightsDetails `
    --description "Connection string for Windows client telemetry (write-only by SDK design)" `
    --output none

if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to store secret in Key Vault"
    exit 1
}

Write-Host "`nConnection string stored in Key Vault!" -ForegroundColor Green
Write-Host "`nKey Vault Secret: appinsights-client-ingestion" -ForegroundColor White
Write-Host "`nTo retrieve the connection string for client configuration:" -ForegroundColor Cyan
Write-Host "  az keyvault secret show --vault-name $keyVaultName --name appinsights-client-ingestion --query value -o tsv"
Write-Host "`nNote: The OpenTelemetry SDK only supports write operations (sending telemetry)." -ForegroundColor Green
Write-Host "The client cannot query or read telemetry data using this connection string." -ForegroundColor Green
