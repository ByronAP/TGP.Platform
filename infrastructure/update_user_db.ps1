$connectionString = "Server=tgp-sql-prod-kt34yxp4xc3hs.database.windows.net;Database=tgp;User Id=tgpadmin;Password=TgpUseR!Prod2025;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$query = "UPDATE tgp.Users SET EmailConfirmed = 1, IsActive = 1 WHERE Email = 'test@tgp.local' OR UserName = 'test@tgp.local'; SELECT UserName, EmailConfirmed, IsActive FROM tgp.Users WHERE UserName = 'test@tgp.local';"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $query
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    $dataset = New-Object System.Data.DataSet
    $adapter.Fill($dataset)
    Write-Host "User Status:"
    $dataset.Tables[0] | Format-Table -AutoSize
    $connection.Close()
}
catch {
    Write-Error $_.Exception.Message
}
