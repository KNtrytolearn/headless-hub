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
            // Detect architecture based on IntPtr.Size and OS description
            var arch = RuntimeInformation.ProcessArchitecture;
            if (arch.ToString() == "X64") return "x64";
            if (arch.ToString() == "X86") return "x86";
            if (arch.ToString() == "Arm") return "arm";
            if (arch.ToString() == "Arm64") return "arm64";
            return "unknown";
        }
    }

    public static string OSName => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
                                   RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Unknown";
}
