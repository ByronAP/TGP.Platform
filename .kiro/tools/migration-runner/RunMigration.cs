using Npgsql;

var connectionString = "Host=ep-red-recipe-a8jzo86k-pooler.eastus2.azure.neon.tech;Database=tgp-data;Username=owner;Password=npg_ptA8fG0dDucH;SSL Mode=Require";

var sql = File.ReadAllText("schema.sql");

// Prepend search_path setting
// Prepend search_path removed to let schema.sql handle it

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();


Console.WriteLine("Connected to database. Running migrations...");

await using var cmd = new NpgsqlCommand(sql, conn);
cmd.CommandTimeout = 300; // 5 minutes timeout
await cmd.ExecuteNonQueryAsync();

Console.WriteLine("Migrations completed successfully!");
