using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TGP.TestDataGenerator.Services;

Console.WriteLine("=================================");
Console.WriteLine("   TGP TEST DATA GENERATOR v2.1  ");
Console.WriteLine("=================================");

// Setup Configuration
var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Setup Logging
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddConsole();
});
var logger = loggerFactory.CreateLogger<Program>();

// Parse Arguments
bool seedOnly = args.Contains("--seed-only");
bool noClean = args.Contains("--no-clean");
List<SeededAgent> agents = new();

if (args.Contains("--seed-test-user"))
{
    Console.WriteLine("\n[3] Seeding Test User (test@tgp.local)...");
    try
    {
        var dataSeeder = new DataSeeder(config, loggerFactory.CreateLogger<DataSeeder>());
        var (userToken, deviceToken, deviceId, userId, tenantId) = await dataSeeder.EnsureTestUserEnvironmentAsync();

        if (deviceId == null)
        {
            logger.LogError("Failed to verify test user environment.");
            return;
        }

        Console.WriteLine($"   User ensured. Device ID: {deviceId}");

        if (string.IsNullOrEmpty(deviceToken))
        {
            logger.LogError("No device token obtained. Cannot seed without valid device authentication.");
            logger.LogError("Ensure the SSO and Gateway services are running and properly configured with matching JWT keys.");
            return;
        }

        var batchSeeder = new BatchSeeder(config, loggerFactory.CreateLogger<BatchSeeder>());
        Console.WriteLine("   Sending historic batches via Gateway API...");
        try {
            await batchSeeder.SeedHistoryAsync(deviceToken, deviceId!);
        } catch (Exception ex) {
            logger.LogError(ex, "History seeding failed");
        }

        Console.WriteLine("   Test user seeding complete.");
        
        // Add to agents list to run simulation
        agents.Add(new SeededAgent(deviceId!, deviceToken, "Desktop"));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding test user");
        return;
    }
}
else
{

int families = 1;
int devices = 2;

if (args.Length > 0 && int.TryParse(args[0], out int f)) families = f;
else if (!Console.IsInputRedirected) {
    Console.Write("Enter number of users to create [1]: ");
    var input = Console.ReadLine();
    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int parsed)) families = parsed;
}

if (args.Length > 1 && int.TryParse(args[1], out int d)) devices = d;
else if (!Console.IsInputRedirected) {
    Console.Write("Enter devices per user [2]: ");
    var input = Console.ReadLine();
    if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int parsed)) devices = parsed;
}




Console.WriteLine("\n[2] Seeding Data...");
try
{
    var seeder = new DataSeeder(config, loggerFactory.CreateLogger<DataSeeder>());
    agents = await seeder.SeedAsync(families, devices);
}
catch (Exception ex)
{
    logger.LogError(ex, "Error during data seeding");
    return;
    }
}

if (seedOnly)
{
    Console.WriteLine("\nSeed only mode completed.");
    return;
}



Console.WriteLine("\n[3] Starting Simulation...");
var simulator = new TrafficSimulator(config["Gateway:Url"] ?? "https://localhost:30080");
simulator.LoadAgents(agents);

Console.WriteLine("\nSimulation running. Press Ctrl+C to stop.");
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => 
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("Stopping...");
};

try
{
    await simulator.RunAsync(cts.Token);
}
catch (OperationCanceledException)
{
    // Ignore
}
catch (Exception ex)
{
    logger.LogError(ex, "Simulation error");
}

Console.WriteLine("Done.");