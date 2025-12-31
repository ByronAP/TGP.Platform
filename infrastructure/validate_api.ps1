$ErrorActionPreference = "Stop"

$ssoUrl = "https://tgp-sso-prod.lemonsand-0fd9cbe0.northcentralus.azurecontainerapps.io"
$gatewayUrl = "https://tgp-gateway-prod.lemonsand-0fd9cbe0.northcentralus.azurecontainerapps.io"

$username = "test@tgp.local"
$password = "User123!"
$deviceId = [Guid]::NewGuid().ToString()

Write-Host "Using Random Device ID: $deviceId"

# 1. Login as User
Write-Host "1. Logging in as User..."
$loginBody = @{
    Username = $username
    Password = $password
    RememberMe = $false
} | ConvertTo-Json

try {
    $loginResponse = Invoke-WebRequest -Uri "$ssoUrl/api/v1/Auth/login" -Method Post -Body $loginBody -ContentType "application/json" -UseBasicParsing
    $content = $loginResponse.Content | ConvertFrom-Json
    $userToken = $content.accessToken
    Write-Host "   User Login successful."
} catch {
    Write-Error "   User Login failed: $_"
    exit 1
}

# 2. Register Device
Write-Host "`n2. Registering Device..."
$registerBody = @{
    DeviceId = $deviceId
    DeviceName = "Test Desktop Manual $deviceId"
    DeviceType = "Desktop"
} | ConvertTo-Json

$registerHeaders = @{
    Authorization = "Bearer $userToken"
}

try {
    $registerResponse = Invoke-WebRequest -Uri "$ssoUrl/api/v1/devices/register" -Method Post -Body $registerBody -ContentType "application/json" -Headers $registerHeaders -UseBasicParsing
    $content = $registerResponse.Content | ConvertFrom-Json
    $deviceToken = $content.accessToken
    Write-Host "   Device Registration successful."
} catch {
    Write-Host "   Device Registration failed."
    if ($_.Exception.Response) {
        Write-Host "   Status Code: $($_.Exception.Response.StatusCode.value__)"
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader $stream
        Write-Host "   Body: $($reader.ReadToEnd())"
    }
    exit 1
}

# 3. Report Detected User
Write-Host "`n3. Reporting Detected User..."
$reportBody = @{
    Username = "detected_user_01"
    SessionId = 1
} | ConvertTo-Json

$reportHeaders = @{
    Authorization = "Bearer $deviceToken"
    "X-Device-Id" = $deviceId
}

try {
    $reportResponse = Invoke-WebRequest -Uri "$gatewayUrl/api/v1/windows/users/detected" -Method Post -Body $reportBody -ContentType "application/json" -Headers $reportHeaders -UseBasicParsing
    $content = $reportResponse.Content | ConvertFrom-Json
    Write-Host "   Report User Response: $($content | ConvertTo-Json -Depth 2)"
    Write-Host "   SUCCESS: User reported."
} catch {
    Write-Host "   Report User failed."
     if ($_.Exception.Response) {
        Write-Host "   Status Code: $($_.Exception.Response.StatusCode.value__)"
        $stream = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader $stream
        Write-Host "   Body: $($reader.ReadToEnd())"
    }
}
