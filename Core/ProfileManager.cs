using HeadlessHub.Core.Model;
using Newtonsoft.Json;

namespace HeadlessHub.Core;

/// <summary>
/// Manages profiles and apps lifecycle
/// </summary>
public class ProfileManager
{
    private readonly string _appsDownloadableFile;
    private readonly string _appsInstallableFile;
    private readonly string _appsLocalFile;
    private readonly string _appsOpenFile;
    private readonly string _profilesFile;

    public List<AppDownloadable> AppsDownloadable { get; private set; } = new();
    public List<AppBase> AppsAll { get; private set; } = new();
    public List<Profile> Profiles { get; private set; } = new();

    public ProfileManager()
    {
        var basePath = Helper.GetAppBasePath();
        _appsDownloadableFile = Path.Combine(basePath, "apps-downloadable.json");
        _appsInstallableFile = Path.Combine(basePath, "apps-installable.json");
        _appsLocalFile = Path.Combine(basePath, "apps-local.json");
        _appsOpenFile = Path.Combine(basePath, "apps-open.json");
        _profilesFile = Path.Combine(basePath, "profiles.json");
    }

    public void LoadAppsAndProfiles()
    {
        AppsDownloadable.Clear();
        AppsAll.Clear();
        Profiles.Clear();

        // Load or create downloadable apps
        if (File.Exists(_appsDownloadableFile))
        {
            var apps = JsonConvert.DeserializeObject<List<AppDownloadable>>(File.ReadAllText(_appsDownloadableFile));
            if (apps != null)
            {
                AppsDownloadable.AddRange(apps);
                AppsAll.AddRange(apps);
            }
        }
        else
        {
            CreateDefaultAppsDownloadable();
        }

        // Load or create profiles
        if (File.Exists(_profilesFile))
        {
            var profiles = JsonConvert.DeserializeObject<List<Profile>>(File.ReadAllText(_profilesFile));
            if (profiles != null)
            {
                Profiles = profiles;
                LinkProfilesToApps();
            }
        }
        else
        {
            CreateDefaultProfiles();
        }
    }

    public void SaveApps()
    {
        var settings = new JsonSerializerSettings { DefaultValueHandling = DefaultValueHandling.Ignore };
        File.WriteAllText(_appsDownloadableFile, JsonConvert.SerializeObject(AppsDownloadable, Formatting.Indented, settings));
        File.WriteAllText(_profilesFile, JsonConvert.SerializeObject(Profiles, Formatting.Indented, settings));
    }

    private void LinkProfilesToApps()
    {
        foreach (var profile in Profiles)
        {
            foreach (var kvp in profile.Apps)
            {
                var app = AppsAll.FirstOrDefault(a => a.Name == kvp.Key);
                if (app != null)
                {
                    kvp.Value.SetApp(app);
                }
                else
                {
                    Console.WriteLine($"[WARN] Profile app '{kvp.Key}' not found");
                }
            }
        }
    }

    private void CreateDefaultAppsDownloadable()
    {
        var os = PlatformHelper.OSName.ToLower();
        var arch = PlatformHelper.Architecture;

        // Darts Caller
        var callerUrl = GetCallerDownloadUrl(os, arch);
        if (!string.IsNullOrEmpty(callerUrl))
        {
            var caller = new AppDownloadable(
                downloadUrl: callerUrl,
                name: "darts-caller",
                descriptionShort: "Calls out thrown points",
                helpUrl: "https://github.com/lbormann/darts-caller",
                changelogUrl: "https://raw.githubusercontent.com/lbormann/darts-caller/master/CHANGELOG.md",
                configuration: new Configuration(
                    prefix: "-",
                    delimiter: " ",
                    arguments: new List<Argument>
                    {
                        new("U", "string", true, section: "Autodarts", nameHuman: "-U / --autodarts_email"),
                        new("P", "password", true, section: "Autodarts", nameHuman: "-P / --autodarts_password"),
                        new("B", "string", true, section: "Autodarts", nameHuman: "-B / --autodarts_board_id"),
                        new("M", "path", true, section: "Media", nameHuman: "-M / --media_path"),
                        new("V", "float[0.0..1.0]", false, section: "Media", nameHuman: "-V / --caller_volume"),
                        new("LPB", "bool", false, section: "Calls", nameHuman: "-LPB / --local_playback",
                            valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" }),
                        new("DEB", "bool", false, section: "Service", nameHuman: "-DEB / --debug",
                            valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" })
                    }
                )
            );
            AppsDownloadable.Add(caller);
        }

        // Darts WLED
        var wledUrl = GetWledDownloadUrl(os, arch);
        if (!string.IsNullOrEmpty(wledUrl))
        {
            var wled = new AppDownloadable(
                downloadUrl: wledUrl,
                name: "darts-wled",
                descriptionShort: "Controls WLED installations by autodarts-events",
                helpUrl: "https://github.com/lbormann/darts-wled",
                changelogUrl: "https://raw.githubusercontent.com/lbormann/darts-wled/master/CHANGELOG.md",
                configuration: new Configuration(
                    prefix: "-",
                    delimiter: " ",
                    arguments: new List<Argument>
                    {
                        new("WEPS", "string", true, isMulti: true, section: "Service", nameHuman: "-WEPS / --wled_endpoints"),
                        new("BRI", "int[1..255]", false, section: "Service", nameHuman: "-BRI / --effect_brightness"),
                        new("IDE", "string", false, section: "Service", nameHuman: "-IDE / --idle_effect"),
                        new("G", "string", false, isMulti: true, section: "Effects", nameHuman: "-G / --game_won_effects"),
                        new("DEB", "bool", false, section: "Service", nameHuman: "-DEB / --debug",
                            valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" })
                    }
                )
            );
            AppsDownloadable.Add(wled);
        }

        // Darts PixelIt
        var pixelitUrl = GetPixelitDownloadUrl(os, arch);
        if (!string.IsNullOrEmpty(pixelitUrl))
        {
            var pixelit = new AppDownloadable(
                downloadUrl: pixelitUrl,
                name: "darts-pixelit",
                descriptionShort: "Controls PIXELIT installations by autodarts-events",
                helpUrl: "https://github.com/lbormann/darts-pixelit",
                changelogUrl: "https://raw.githubusercontent.com/lbormann/darts-pixelit/main/CHANGELOG.md",
                configuration: new Configuration(
                    prefix: "-",
                    delimiter: " ",
                    arguments: new List<Argument>
                    {
                        new("PEPS", "string", true, isMulti: true, section: "PixelIt", nameHuman: "-PEPS / --pixelit_endpoints"),
                        new("TP", "path", true, section: "PixelIt", nameHuman: "-TP / --templates_path"),
                        new("BRI", "int[1..255]", false, section: "PixelIt", nameHuman: "-BRI / --effect_brightness"),
                        new("IDE", "string", false, isMulti: true, section: "PixelIt", nameHuman: "-IDE / --idle_effects"),
                        new("G", "string", false, isMulti: true, section: "PixelIt", nameHuman: "-G / --game_won_effects"),
                        new("DEB", "bool", false, section: "Service", nameHuman: "-DEB / --debug",
                            valueMapping: new Dictionary<string, string> { ["True"] = "1", ["False"] = "0" })
                    }
                )
            );
            AppsDownloadable.Add(pixelit);
        }

        AppsAll.AddRange(AppsDownloadable);
        SaveApps();

        Console.WriteLine($"[INFO] Created {AppsDownloadable.Count} default apps for {os}-{arch}");
    }

    private void CreateDefaultProfiles()
    {
        var apps = new Dictionary<string, ProfileState>();

        if (AppsDownloadable.Any(a => a.Name == "darts-caller"))
            apps.Add("darts-caller", new ProfileState(false, true));

        if (AppsDownloadable.Any(a => a.Name == "darts-wled"))
            apps.Add("darts-wled", new ProfileState());

        if (AppsDownloadable.Any(a => a.Name == "darts-pixelit"))
            apps.Add("darts-pixelit", new ProfileState());

        Profiles.Add(new Profile("default", apps));
        LinkProfilesToApps();
        SaveApps();
    }

    private string? GetCallerDownloadUrl(string os, string arch)
    {
        var version = "v2.20.3";
        return (os, arch) switch
        {
            ("windows", "x64") => $"https://github.com/lbormann/darts-caller/releases/download/{version}/darts-caller.exe",
            ("linux", "x64") => $"https://github.com/lbormann/darts-caller/releases/download/{version}/darts-caller",
            ("linux", "arm64") => $"https://github.com/lbormann/darts-caller/releases/download/{version}/darts-caller-arm64",
            ("macos", "x64") => $"https://github.com/lbormann/darts-caller/releases/download/{version}/darts-caller-macx64",
            ("macos", "arm64") => $"https://github.com/lbormann/darts-caller/releases/download/{version}/darts-caller-mac",
            _ => null
        };
    }

    private string? GetWledDownloadUrl(string os, string arch)
    {
        var version = "v1.10.4";
        return (os, arch) switch
        {
            ("windows", "x64") => $"https://github.com/lbormann/darts-wled/releases/download/{version}/darts-wled.exe",
            ("linux", "x64") => $"https://github.com/lbormann/darts-wled/releases/download/{version}/darts-wled",
            ("linux", "arm64") => $"https://github.com/lbormann/darts-wled/releases/download/{version}/darts-wled-arm64",
            ("macos", "x64") => $"https://github.com/lbormann/darts-wled/releases/download/{version}/darts-wled-mac64",
            ("macos", "arm64") => $"https://github.com/lbormann/darts-wled/releases/download/{version}/darts-wled-mac",
            _ => null
        };
    }

    private string? GetPixelitDownloadUrl(string os, string arch)
    {
        var version = "v1.3.1";
        return (os, arch) switch
        {
            ("windows", "x64") => $"https://github.com/lbormann/darts-pixelit/releases/download/{version}/darts-pixelit.exe",
            ("linux", "x64") => $"https://github.com/lbormann/darts-pixelit/releases/download/{version}/darts-pixelit",
            ("linux", "arm64") => $"https://github.com/lbormann/darts-pixelit/releases/download/{version}/darts-pixelit-arm64",
            ("macos", "x64") => $"https://github.com/lbormann/darts-pixelit/releases/download/{version}/darts-pixelit-mac",
            ("macos", "arm64") => $"https://github.com/lbormann/darts-pixelit/releases/download/{version}/darts-pixelit-mac",
            _ => null
        };
    }

    public void RunProfile(string profileName)
    {
        var profile = Profiles.FirstOrDefault(p => p.Name == profileName);
        if (profile == null)
        {
            Console.WriteLine($"[WARN] Profile '{profileName}' not found");
            return;
        }

        Console.WriteLine($"[INFO] Starting profile: {profileName}");

        foreach (var kvp in profile.Apps.Where(a => a.Value.TaggedForStart))
        {
            try
            {
                Console.WriteLine($"[INFO] Starting app: {kvp.Key}");
                kvp.Value.App?.Run(kvp.Value.RuntimeArguments);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to start {kvp.Key}: {ex.Message}");
            }
        }
    }

    public void StopProfile(string profileName)
    {
        var profile = Profiles.FirstOrDefault(p => p.Name == profileName);
        if (profile == null) return;

        Console.WriteLine($"[INFO] Stopping profile: {profileName}");

        foreach (var kvp in profile.Apps)
        {
            try
            {
                kvp.Value.App?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to stop {kvp.Key}: {ex.Message}");
            }
        }
    }

    public void StopAllApps()
    {
        foreach (var app in AppsAll)
        {
            try { app.Close(); }
            catch { }
        }
    }
}
