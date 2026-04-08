using HeadlessHub.Core.Control;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace HeadlessHub.Core.Model;

/// <summary>
/// Main functions for using an app
/// </summary>
public abstract class AppBase
{
    public const int MaxAppMonitorEntries = 600;

    [JsonProperty("Name")]
    public string Name { get; set; }
    
    [JsonProperty("CustomName")]
    public string CustomName { get; set; }
    
    [JsonProperty("HelpUrl")]
    public string? HelpUrl { get; set; }
    
    [JsonProperty("ChangelogUrl")]
    public string? ChangelogUrl { get; set; }
    
    [JsonProperty("DescriptionShort")]
    public string? DescriptionShort { get; set; }
    
    [JsonProperty("DescriptionLong")]
    public string? DescriptionLong { get; protected set; }
    
    [JsonProperty("RunAsAdmin")]
    public bool RunAsAdmin { get; protected set; }
    
    [JsonProperty("Chmod")]
    public bool Chmod { get; set; }
    
    [JsonProperty("StartWindowState")]
    public ProcessWindowStyle StartWindowState { get; protected set; }
    
    [JsonProperty("Configuration")]
    public Configuration? Configuration { get; protected set; }

    [JsonIgnore]
    public bool AppRunningState { get; protected set; }

    [JsonIgnore]
    public string AppMonitor { get; protected set; } = string.Empty;

    [JsonIgnore]
    public string AppConsoleStdOutput { get; protected set; } = string.Empty;

    [JsonIgnore]
    public string AppConsoleStdError { get; protected set; } = string.Empty;

    [JsonIgnore]
    protected Process? _process;
    
    [JsonIgnore]
    protected int _processId;
    
    [JsonIgnore]
    protected Dictionary<string, string>? _runtimeArguments;

    public AppBase(string name, string? customName = null, string? helpUrl = null,
        string? changelogUrl = null, string? descriptionShort = null, string? descriptionLong = null,
        bool runAsAdmin = false, bool chmod = true, ProcessWindowStyle? startWindowState = null,
        Configuration? configuration = null)
    {
        Name = name;
        CustomName = customName ?? name;
        HelpUrl = helpUrl;
        ChangelogUrl = changelogUrl;
        DescriptionShort = descriptionShort;
        DescriptionLong = descriptionLong;
        RunAsAdmin = runAsAdmin;
        Chmod = chmod;
        StartWindowState = startWindowState ?? ProcessWindowStyle.Minimized;
        Configuration = configuration;
        _processId = 0;
        AppRunningState = false;
    }

    public bool Run(Dictionary<string, string>? runtimeArguments = null)
    {
        _runtimeArguments = runtimeArguments;
        var executable = SetRunExecutable();
        if (IsRunning()) return true;
        if (Install()) return false;
        RunProcess(runtimeArguments);
        return true;
    }

    public bool ReRun(Dictionary<string, string>? runtimeArguments = null)
    {
        if (!IsRunning()) return false;
        Close();
        return Run(runtimeArguments);
    }

    public bool IsInstalled()
    {
        var executable = SetRunExecutable();
        return !string.IsNullOrEmpty(executable) && File.Exists(executable);
    }

    public bool IsRunning() => AppRunningState;

    public bool IsConfigurationChanged() => Configuration?.IsChanged() ?? false;

    public void Close()
    {
        if (IsRunning())
        {
            try
            {
                _process?.CloseMainWindow();
                _process?.Close();
                
                if (_processId > 0)
                {
                    Helper.KillProcess(_processId);
                }
                
                var executable = SetRunExecutable();
                if (!string.IsNullOrEmpty(executable))
                {
                    Helper.KillProcess(executable);
                }
                
                AppRunningState = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Can't close {Name}: {ex.Message}");
            }
        }
    }

    public abstract bool Install();
    public abstract bool IsConfigurable();
    public abstract bool IsInstallable();
    protected abstract string? SetRunExecutable();

    protected async void RunProcess(Dictionary<string, string>? runtimeArguments = null)
    {
        var executable = SetRunExecutable();
        if (string.IsNullOrEmpty(executable)) return;

        var arguments = ComposeArguments(this, runtimeArguments);
        if (arguments == null) return;

        try
        {
            AppConsoleStdOutput = string.Empty;
            AppConsoleStdError = string.Empty;
            AppMonitor = string.Empty;

            _process = new Process();
            _process.StartInfo.WindowStyle = StartWindowState;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.UseShellExecute = false;

            _process.Exited += (sender, e) =>
            {
                Console.WriteLine($"[INFO] Process {Name} exited");
                _processId = 0;
                AppRunningState = false;
            };

            _process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppConsoleStdOutput += e.Data + Environment.NewLine;
                    AppMonitor = AppConsoleStdOutput + Environment.NewLine + AppConsoleStdError;
                    Console.WriteLine($"[{Name}] {e.Data}");
                }
            };

            _process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    AppConsoleStdError += e.Data + Environment.NewLine;
                    AppMonitor = AppConsoleStdOutput + Environment.NewLine + AppConsoleStdError;
                    Console.WriteLine($"[{Name}] ERROR: {e.Data}");
                }
            };

            _process.StartInfo.FileName = executable;
            _process.StartInfo.Arguments = arguments;
            _process.StartInfo.WorkingDirectory = Path.GetDirectoryName(executable) ?? string.Empty;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && RunAsAdmin)
            {
                _process.StartInfo.Verb = "runas";
            }
            else if ((RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) && Chmod)
            {
                EnsureExecutablePermissions(executable);
            }

            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            _processId = _process.Id;
            AppRunningState = true;

            Console.WriteLine($"[INFO] Started {Name} with PID {_processId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to start {executable}: {ex.Message}");
        }
    }

    private void EnsureExecutablePermissions(string scriptPath)
    {
        var chmodProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{scriptPath}\"",
                UseShellExecute = false
            }
        };
        chmodProcess.Start();
        chmodProcess.WaitForExit();
    }

    protected string? ComposeArguments(AppBase app, Dictionary<string, string>? runtimeArguments = null)
    {
        try
        {
            return IsConfigurable() ? Configuration?.GenerateArgumentString(app, runtimeArguments) : "";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] {ex.Message}");
            return null;
        }
    }
}
