using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

namespace HeadlessHub.Core;

/// <summary>
/// Platform detection helper
/// </summary>
public static class PlatformHelper
{
    public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    
    public static string Architecture => RuntimeInformation.ProcessArchitecture switch
    {
        Architecture.X64 => "x64",
        Architecture.X86 => "x86",
        Architecture.Arm => "arm",
        Architecture.Arm64 => "arm64",
        _ => "unknown"
    };

    public static string OSName => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
                                   RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "macOS" : "Unknown";
}
