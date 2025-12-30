
# Connect and list tables
$connectionString = "Server=tgp-sql-prod-kt34yxp4xc3hs.database.windows.net;Database=tgp;User Id=tgpadmin;Password=TgpUseR!Prod2025;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'tgp' ORDER BY TABLE_NAME;"

try {
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    $command = $connection.CreateCommand()
    $command.CommandText = $query
    $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($command)
    $dataset = New-Object System.Data.DataSet
    $adapter.Fill($dataset)
    $dataset.Tables[0] | Format-Table -AutoSize
    $connection.Close()
}
catch {
    Write-Error $_.Exception.Message
}
