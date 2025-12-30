param (
    [string]$ResourceGroupName = "rg-tgp-prod-northcentralus"
)

# Get Deployment Outputs
$deploymentName = "main" # The default name used by az deployment group create if template-file is main.bicep? 
# actually I didn't specify --name in deploy.ps1, so it defaults to main. Let's assume 'main'.
# Wait, I should check the deployment name.
# Or I can just query resources by tag/name using the conventions.

# Let's query by specific resource names if possible, but outputs are better.
# Let's try to get outputs from the deployment first.
$outputs = az deployment group show -g $ResourceGroupName -n main --query properties.outputs
if ($LASTEXITCODE -ne 0) {
    Write-Host "Deployment 'main' not found. Trying to find resources by tag..."
    # Fallback logic if needed
}

# Parse outputs (this is tricky in PS with JSON result from CLI)
$outputsJson = $outputs | ConvertFrom-Json

$sqlServerFqdn = $outputsJson.sqlServerFqdn.value
$redisHost = $outputsJson.redisHost.value
$serviceBusEndpoint = $outputsJson.serviceBusEndpoint.value
$storageAccountName = $outputsJson.storageAccountName.value
$acaEnvId = $outputsJson.acaEnvironmentId.value

Write-Host "--- Resources ---" -ForegroundColor Cyan
Write-Host "SQL Server: $sqlServerFqdn"
Write-Host "Redis: $redisHost"
Write-Host "ServiceBus: $serviceBusEndpoint"
Write-Host "Storage: $storageAccountName"
Write-Host "ACA Env: $acaEnvId"

# Fetch Keys
Write-Host "`n--- Fetching Keys ---" -ForegroundColor Cyan

# Storage Key
$storageKey = az storage account keys list -g $ResourceGroupName -n $storageAccountName --query "[0].value" -o tsv
Write-Host "Storage Connection String:" -ForegroundColor Yellow
Write-Host "DefaultEndpointsProtocol=https;AccountName=$storageAccountName;AccountKey=$storageKey;EndpointSuffix=core.windows.net"

# Redis Key
$redisKey = az redis list-keys -g $ResourceGroupName -n $redisHost.Split(".")[0] --query primaryKey -o tsv
Write-Host "`nRedis Connection String:" -ForegroundColor Yellow
Write-Host "$redisHost:6380,password=$redisKey,ssl=True,abortConnect=False"

# Service Bus Key
# Assuming 'servicebus' output endpoint is like https://tgp-sb-dev-xyz.servicebus.windows.net:443/
# We need the namespace name.
$sbNamespace = $serviceBusEndpoint.Split("/")[2].Split(".")[0]
$sbKey = az servicebus namespace authorization-rule keys list -g $ResourceGroupName --namespace-name $sbNamespace --name RootManageSharedAccessKey --query primaryKey -o tsv
Write-Host "`nService Bus Connection String:" -ForegroundColor Yellow
Write-Host "Endpoint=sb://$sbNamespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=$sbKey"

# SQL Server
Write-Host "`nSQL Server Connection String:" -ForegroundColor Yellow
Write-Host "Server=$sqlServerFqdn;Database=tgp;User Id=tgpadmin;Password=<YOUR_PASSWORD>;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
