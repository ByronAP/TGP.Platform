$connectionString = "Server=tgp-sql-prod-kt34yxp4xc3hs.database.windows.net;Database=tgp;User Id=tgpadmin;Password=TgpUseR!Prod2025;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

$query = @"
SELECT 'Links' as Source, DeviceId, Username, ProfileId FROM tgp.MonitoredProfileLinks
UNION ALL
SELECT 'Users' as Source, DeviceId, Username, NULL as ProfileId FROM tgp.MonitoredUsers
Query;
"@

Invoke-Sqlcmd -ConnectionString $connectionString -Query "SELECT * FROM tgp.MonitoredProfileLinks" | Format-Table -AutoSize
Invoke-Sqlcmd -ConnectionString $connectionString -Query "SELECT * FROM tgp.MonitoredUsers" | Format-Table -AutoSize
Invoke-Sqlcmd -ConnectionString $connectionString -Query "SELECT * FROM tgp.Devices" | Format-Table -AutoSize
