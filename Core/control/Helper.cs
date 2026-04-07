using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Http;

namespace HeadlessHub.Core.Control;

/// <summary>
/// Provides common functions
/// </summary>
public static class Helper
{
    public static bool DirectoryOrFileStartsWith(string path, string searchString)
    {
        if (Directory.Exists(path) && Path.GetFileName(path).StartsWith(searchString))
            return true;

        if (Directory.Exists(path))
        {
            foreach (var file in Directory.GetFiles(path))
            {
                if (Path.GetFileName(file).StartsWith(searchString))
                    return true;
            }
        }

        return false;
    }

    public static long GetFileSizeByUrl(string url)
    {
        try
        {
            var req = WebRequest.Create(url);
            req.Method = "HEAD";
            req.Timeout = 4000;
            using var resp = req.GetResponse();
            if (long.TryParse(resp.Headers.Get("Content-Length"), out var contentLength))
                return contentLength;
        }
        catch { }
        return -1;
    }

    public static long GetFileSizeByLocal(string pathToFile)
    {
        if (File.Exists(pathToFile))
            return new FileInfo(pathToFile).Length;
        return -2;
    }

    public static async Task<string> AsyncHttpGet(string url, double timeout)
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(timeout);
            var response = await client.GetAsync(url);
            return response.IsSuccessStatusCode ? await response.Content.ReadAsStringAsync() : string.Empty;
        }
        catch { }
        return string.Empty;
    }

    public static string GetAppBasePath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var executablePath = Process.GetCurrentProcess().MainModule?.FileName;
            return Path.GetDirectoryName(executablePath) ?? AppContext.BaseDirectory;
        }
        return Path.GetDirectoryName(AppContext.BaseDirectory) ?? AppContext.BaseDirectory;
    }

    public static string GetUserDirectoryPath()
    {
        var path = Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))?.FullName;
        if (Environment.OSVersion.Version.Major >= 6 && path != null)
        {
            path = Directory.GetParent(path)?.ToString();
        }
        return path ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    public static void RemoveDirectory(string directory, bool createAfterRemove = false)
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
        if (createAfterRemove)
            Directory.CreateDirectory(directory);
    }

    public static string GetFileNameByUrl(string url)
    {
        var parts = url.Split("/");
        return parts[^1];
    }

    public static string? SearchExecutable(string path)
    {
        if (!Directory.Exists(path)) return null;

        string[] executableExtensions;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            executableExtensions = new[] { "exe" };
        }
        else
        {
            executableExtensions = new[] { "" }; // No extension on Linux/macOS
        }

        return Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories)
            .FirstOrDefault(s => executableExtensions.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));
    }

    public static bool IsProcessRunning(int processId)
    {
        try
        {
            return processId > 0 && Process.GetProcessById(processId) != null;
        }
        catch { return false; }
    }

    public static bool IsProcessRunning(string? processName)
    {
        if (string.IsNullOrEmpty(processName)) return false;
        processName = Path.GetFileNameWithoutExtension(processName);
        return Process.GetProcesses().Any(p => p.ProcessName.Contains(processName, StringComparison.OrdinalIgnoreCase));
    }

    public static void KillProcess(int processId)
    {
        if (processId <= 0) return;
        try
        {
            var process = Process.GetProcessById(processId);
            process.Kill(false);
        }
        catch { }
    }

    public static void KillProcess(string? processName)
    {
        if (string.IsNullOrEmpty(processName)) return;
        processName = Path.GetFileNameWithoutExtension(processName);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            KillProcessesByNameOsX(processName);
        }
        else
        {
            foreach (var process in Process.GetProcessesByName(processName))
            {
                try { process.Kill(false); } catch { }
            }
        }
    }

    private static void KillProcessesByNameOsX(string processName)
    {
        var processIds = FindProcessesByNameOsx(processName);
        foreach (var pid in processIds.Reverse())
        {
            KillProcess(pid);
        }
    }

    private static int[] FindProcessesByNameOsx(string processName)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"pgrep {processName}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.Parse(id))
                .ToArray();
        }
        catch { return Array.Empty<int>(); }
    }
}
