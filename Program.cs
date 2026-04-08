using HeadlessHub.Core;
using HeadlessHub.Services;
using HeadlessHub.WebApi;

namespace HeadlessHub;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("==========================================");
        Console.WriteLine("       HeadlessHub for autodarts.io      ");
        Console.WriteLine("     RK3528 Darts Extension Manager      ");
        Console.WriteLine("==========================================");
        Console.WriteLine();

        var dataDir = Path.Combine(Environment.CurrentDirectory, "data");
        if (!Directory.Exists(dataDir))
        {
            Directory.CreateDirectory(dataDir);
        }

        // Initialize Profile Manager
        var profileManager = new ProfileManager();
        profileManager.LoadAppsAndProfiles();
        
        Console.WriteLine($"[INFO] Loaded {profileManager.Profiles.Count} profiles");
        Console.WriteLine($"[INFO] Available apps: {string.Join(", ", profileManager.AppsAll.Select(a => a.Name))}");
        Console.WriteLine();

        // Build Web API
        var builder = WebApplication.CreateBuilder(args);
        
        // Configure for headless environment
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5000); // HTTP port for local network access
        });

        // Add services
        builder.Services.AddSingleton(profileManager);
        builder.Services.AddSingleton<WebSocketLoggerService>();
        builder.Services.AddHostedService<WebSocketLoggerService>(sp => sp.GetRequiredService<WebSocketLoggerService>());

        var app = builder.Build();

        // Configure API endpoints
        app.MapEndpoints();

        Console.WriteLine("[INFO] Starting Web API on http://0.0.0.0:5000");
        Console.WriteLine("[INFO] Access from browser: http://<RK3528-IP>:5000");
        Console.WriteLine();

        await app.RunAsync();
    }
}
