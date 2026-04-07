using HeadlessHub.Core;
using HeadlessHub.Core.Model;
using HeadlessHub.Services;
using System.Net.WebSockets;
using System.Text.Json;

namespace HeadlessHub.WebApi;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        // Status
        app.MapGet("/api/status", (ProfileManager pm) =>
        {
            return Results.Ok(new
            {
                platform = PlatformHelper.OSName,
                architecture = PlatformHelper.Architecture,
                profilesCount = pm.Profiles.Count,
                appsCount = pm.AppsAll.Count,
                runningApps = pm.AppsAll.Count(a => a.IsRunning()),
                timestamp = DateTime.UtcNow
            });
        });

        // Profiles
        app.MapGet("/api/profiles", (ProfileManager pm) =>
        {
            return Results.Ok(pm.Profiles.Select(p => new
            {
                p.Name,
                p.IsTaggedForStart,
                Apps = p.Apps.Select(a => new
                {
                    Name = a.Key,
                    a.Value.IsRequired,
                    a.Value.TaggedForStart,
                    IsRunning = a.Value.App?.IsRunning() ?? false
                })
            }));
        });

        app.MapGet("/api/profiles/{name}", (string name, ProfileManager pm) =>
        {
            var profile = pm.Profiles.FirstOrDefault(p => p.Name == name);
            if (profile == null) return Results.NotFound();

            return Results.Ok(new
            {
                profile.Name,
                profile.IsTaggedForStart,
                Apps = profile.Apps.Select(a => new
                {
                    Name = a.Key,
                    a.Value.IsRequired,
                    a.Value.TaggedForStart,
                    IsRunning = a.Value.App?.IsRunning() ?? false,
                    RuntimeArguments = a.Value.RuntimeArguments
                })
            });
        });

        app.MapPost("/api/profiles/{name}/start", async (string name, ProfileManager pm) =>
        {
            var profile = pm.Profiles.FirstOrDefault(p => p.Name == name);
            if (profile == null) return Results.NotFound($"Profile '{name}' not found");

            pm.RunProfile(name);
            return Results.Ok(new { message = $"Profile '{name}' started" });
        });

        app.MapPost("/api/profiles/{name}/stop", (string name, ProfileManager pm) =>
        {
            var profile = pm.Profiles.FirstOrDefault(p => p.Name == name);
            if (profile == null) return Results.NotFound($"Profile '{name}' not found");

            pm.StopProfile(name);
            return Results.Ok(new { message = $"Profile '{name}' stopped" });
        });

        // Apps
        app.MapGet("/api/apps", (ProfileManager pm) =>
        {
            return Results.Ok(pm.AppsAll.Select(a => new
            {
                a.Name,
                a.CustomName,
                a.DescriptionShort,
                a.HelpUrl,
                IsInstalled = a.IsInstalled(),
                IsRunning = a.IsRunning(),
                IsConfigurable = a.IsConfigurable(),
                IsInstallable = a.IsInstallable()
            }));
        });

        app.MapGet("/api/apps/{name}", (string name, ProfileManager pm) =>
        {
            var app = pm.AppsAll.FirstOrDefault(a => a.Name == name);
            if (app == null) return Results.NotFound($"App '{name}' not found");

            return Results.Ok(new
            {
                app.Name,
                app.CustomName,
                app.DescriptionShort,
                app.DescriptionLong,
                app.HelpUrl,
                app.ChangelogUrl,
                IsInstalled = app.IsInstalled(),
                IsRunning = app.IsRunning(),
                IsConfigurable = app.IsConfigurable(),
                IsInstallable = app.IsInstallable(),
                Configuration = app.Configuration != null ? new
                {
                    app.Configuration.Prefix,
                    app.Configuration.Delimiter,
                    Arguments = app.Configuration.Arguments.Select(arg => new
                    {
                        arg.Name,
                        arg.NameHuman,
                        arg.Type,
                        arg.Required,
                        arg.Section,
                        arg.Description,
                        arg.Value,
                        arg.IsMulti
                    })
                } : null
            });
        });

        app.MapPost("/api/apps/{name}/download", async (string name, ProfileManager pm) =>
        {
            var app = pm.AppsAll.FirstOrDefault(a => a.Name == name);
            if (app == null) return Results.NotFound($"App '{name}' not found");

            if (app is not AppDownloadable downloadable)
                return Results.BadRequest("App is not downloadable");

            try
            {
                downloadable.Install();
                pm.SaveApps();
                return Results.Ok(new { message = $"App '{name}' downloaded successfully" });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to download: {ex.Message}");
            }
        });

        app.MapPost("/api/apps/{name}/run", (string name, ProfileManager pm) =>
        {
            var app = pm.AppsAll.FirstOrDefault(a => a.Name == name);
            if (app == null) return Results.NotFound($"App '{name}' not found");

            try
            {
                app.Run();
                return Results.Ok(new { message = $"App '{name}' started" });
            }
            catch (Exception ex)
            {
                return Results.Problem($"Failed to start: {ex.Message}");
            }
        });

        app.MapPost("/api/apps/{name}/stop", (string name, ProfileManager pm) =>
        {
            var app = pm.AppsAll.FirstOrDefault(a => a.Name == name);
            if (app == null) return Results.NotFound($"App '{name}' not found");

            app.Close();
            return Results.Ok(new { message = $"App '{name}' stopped" });
        });

        app.MapPut("/api/apps/{name}/config", async (string name, HttpContext ctx, ProfileManager pm) =>
        {
            var app = pm.AppsAll.FirstOrDefault(a => a.Name == name);
            if (app == null) return Results.NotFound($"App '{name}' not found");

            if (app.Configuration == null)
                return Results.BadRequest("App is not configurable");

            var body = await ctx.Request.ReadFromJsonAsync<Dictionary<string, string>>();
            if (body == null) return Results.BadRequest("Invalid config body");

            foreach (var kvp in body)
            {
                var arg = app.Configuration.Arguments.FirstOrDefault(a => a.Name == kvp.Key);
                if (arg != null)
                {
                    arg.Value = kvp.Value;
                }
            }

            pm.SaveApps();
            return Results.Ok(new { message = $"Config updated for '{name}'" });
        });

        // WebSocket for logs
        app.MapGet("/ws/logs", async (HttpContext ctx, WebSocketLoggerService logger) =>
        {
            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = 400;
                return;
            }

            var socket = await ctx.WebSockets.AcceptWebSocketAsync();
            logger.AddClient(socket);

            var buffer = new byte[1024];
            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }
                }
            }
            finally
            {
                logger.RemoveClient(socket);
            }
        });

        // Serve static files (simple Web UI)
        app.MapGet("/", async (HttpContext ctx) =>
        {
            var wwwroot = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var indexPath = Path.Combine(wwwroot, "index.html");

            if (File.Exists(indexPath))
            {
                ctx.Response.ContentType = "text/html";
                await ctx.Response.SendFileAsync(indexPath);
            }
            else
            {
                ctx.Response.ContentType = "text/html";
                await ctx.Response.WriteAsync(GetDefaultHtml());
            }
        });
    }

    private static string GetDefaultHtml()
    {
        return """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>HeadlessHub</title>
    <style>
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #1a1a2e; color: #eee; padding: 20px; }
        h1 { margin-bottom: 20px; color: #00d9ff; }
        h2 { margin: 20px 0 10px; color: #00d9ff; font-size: 18px; }
        .card { background: #16213e; border-radius: 8px; padding: 15px; margin-bottom: 15px; }
        .app { display: flex; justify-content: space-between; align-items: center; padding: 10px; border-bottom: 1px solid #333; }
        .app:last-child { border-bottom: none; }
        .status { font-size: 12px; padding: 2px 8px; border-radius: 4px; }
        .status.running { background: #0f0; color: #000; }
        .status.stopped { background: #333; color: #888; }
        .status.installed { background: #00d9ff; color: #000; }
        button { background: #00d9ff; color: #000; border: none; padding: 8px 16px; border-radius: 4px; cursor: pointer; margin: 2px; }
        button:hover { background: #00b8d9; }
        button.stop { background: #ff4757; }
        button.stop:hover { background: #ff3344; }
        #logs { background: #0f0f23; padding: 15px; border-radius: 8px; height: 300px; overflow-y: auto; font-family: monospace; font-size: 12px; }
        .log-line { margin: 2px 0; }
        .log-error { color: #ff4757; }
        .log-info { color: #00d9ff; }
    </style>
</head>
<body>
    <h1>🎯 HeadlessHub</h1>
    
    <div class="card">
        <h2>Status</h2>
        <div id="status">Loading...</div>
    </div>

    <div class="card">
        <h2>Profiles</h2>
        <div id="profiles">Loading...</div>
    </div>

    <div class="card">
        <h2>Apps</h2>
        <div id="apps">Loading...</div>
    </div>

    <div class="card">
        <h2>Logs</h2>
        <div id="logs"></div>
    </div>

    <script>
        const API = '/api';
        
        async function fetchJSON(url) {
            const res = await fetch(url);
            return res.json();
        }
        
        async function post(url) {
            await fetch(url, { method: 'POST' });
            refresh();
        }
        
        async function refresh() {
            const status = await fetchJSON(`${API}/status`);
            document.getElementById('status').innerHTML = 
                `Platform: ${status.platform} (${status.architecture}) | ` +
                `Running: ${status.runningApps}/${status.appsCount}`;
            
            const profiles = await fetchJSON(`${API}/profiles`);
            document.getElementById('profiles').innerHTML = profiles.map(p => 
                `<div class="app">
                    <span>${p.name}</span>
                    <span>
                        <button onclick="post('${API}/profiles/${p.name}/start')">Start</button>
                        <button class="stop" onclick="post('${API}/profiles/${p.name}/stop')">Stop</button>
                    </span>
                </div>`
            ).join('');
            
            const apps = await fetchJSON(`${API}/apps`);
            document.getElementById('apps').innerHTML = apps.map(a => 
                `<div class="app">
                    <span>
                        <strong>${a.customName}</strong><br>
                        <small>${a.descriptionShort || ''}</small>
                    </span>
                    <span>
                        <span class="status ${a.isRunning ? 'running' : 'stopped'}">${a.isRunning ? 'Running' : 'Stopped'}</span>
                        <span class="status ${a.isInstalled ? 'installed' : ''}">${a.isInstalled ? 'Installed' : 'Not Installed'}</span>
                        ${a.isInstallable && !a.isInstalled ? `<button onclick="post('${API}/apps/${a.name}/download')">Download</button>` : ''}
                        ${a.isInstalled && !a.isRunning ? `<button onclick="post('${API}/apps/${a.name}/run')">Run</button>` : ''}
                        ${a.isRunning ? `<button class="stop" onclick="post('${API}/apps/${a.name}/stop')">Stop</button>` : ''}
                    </span>
                </div>`
            ).join('');
        }
        
        // WebSocket for logs
        const ws = new WebSocket(`ws://${location.host}/ws/logs`);
        ws.onmessage = (e) => {
            const logs = document.getElementById('logs');
            const line = document.createElement('div');
            line.className = 'log-line';
            line.textContent = e.data;
            if (e.data.includes('ERROR')) line.classList.add('log-error');
            if (e.data.includes('INFO')) line.classList.add('log-info');
            logs.appendChild(line);
            logs.scrollTop = logs.scrollHeight;
        };
        
        refresh();
        setInterval(refresh, 5000);
    </script>
</body>
</html>
""";
    }
}
