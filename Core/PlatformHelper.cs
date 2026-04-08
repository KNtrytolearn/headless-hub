using System.Runtime.InteropServices;

namespace HeadlessHub.Core;

/// <summary>
/// Platform detection helper
/// </summary>
public static class PlatformHelper
{
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    
    public static string Architecture
    {
        get
        {
            var arch = RuntimeInformation.ProcessArchitecture;
            if (arch == System.Reflection.Architecture.X64) return "x64";
            if (arch == System.Reflection.Architecture.X86) return "x86";
            if (arch == System.Reflection.Architecture.Arm) return "arm";
            if (arch == System.Reflection.Architecture.Arm64) return "arm64";
            return "unknown";
        }
    }

    public static string OSName => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
                                   RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Unknown";
}
