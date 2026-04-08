using HeadlessHub.Core;
using HeadlessHub.Services;
using HeadlessHub.WebApi;

namespace HeadlessHub;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("鈺斺晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晽");
        Console.WriteLine("鈺?      HeadlessHub for autodarts.io       鈺?);
        Console.WriteLine("鈺?    RK3528 Darts Extension Manager       鈺?);
        Console.WriteLine("鈺氣晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨晲鈺愨暆");
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
