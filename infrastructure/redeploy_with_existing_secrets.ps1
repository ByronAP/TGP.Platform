$ResourceGroupName = "rg-tgp-prod-northcentralus"

Write-Host "Finding Key Vault..."
$kvName = az keyvault list -g $ResourceGroupName --query "[0].name" -o tsv
if (-not $kvName) {
    Write-Error "Key Vault not found in $ResourceGroupName. Is this the first deployment?"
    exit 1
}
Write-Host "Found Key Vault: $kvName"

Write-Host "Retrieving secrets..."
$jwtKey = az keyvault secret show --vault-name $kvName --name "Jwt--SecretKey" --query value -o tsv
$connStr = az keyvault secret show --vault-name $kvName --name "ConnectionStrings--DefaultConnection" --query value -o tsv

# Parse Password from Connection String
# Format: Server=...;Password=YOUR_PASSWORD;...
$dbPassword = ""
if ($connStr -match "Password=([^;]+)") {
    $dbPassword = $matches[1]
} else {
    Write-Error "Could not parse Password from connection string."
    exit 1
}

Write-Host "Secrets retrieved successfully."

Write-Host "Starting Deployment..."
./deploy.ps1 -ResourceGroupName $ResourceGroupName -DbPassword (ConvertTo-SecureString $dbPassword -AsPlainText -Force) -JwtSecretKey (ConvertTo-SecureString $jwtKey -AsPlainText -Force)
