using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Http;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace HeadlessHub.Core.Model;

/// <summary>
/// App that can be downloaded from the internet
/// </summary>
public class AppDownloadable : AppBase
{
    public string DownloadUrl { get; set; }

    protected string _downloadPath;
    protected string _downloadPathFile;
    protected bool _skipRun;

    public AppDownloadable(string downloadUrl, string name, string? customName = null,
        string? helpUrl = null, string? changelogUrl = null, string? descriptionShort = null,
        string? descriptionLong = null, bool runAsAdmin = false, bool chmod = true,
        ProcessWindowStyle? startWindowState = null, Configuration? configuration = null)
        : base(name, customName, helpUrl, changelogUrl, descriptionShort, descriptionLong,
            runAsAdmin, chmod, startWindowState, configuration)
    {
        DownloadUrl = downloadUrl;
        GeneratePaths();
    }

    public override bool Install()
    {
        try
        {
            // Check if download is needed
            if (Helper.DirectoryOrFileStartsWith(_downloadPath, "my_version"))
                return false;

            var urlFileSize = Helper.GetFileSizeByUrl(DownloadUrl);
            var localFileSize = Helper.GetFileSizeByLocal(_downloadPathFile);

            _skipRun = localFileSize == -2 ? false : true;

            if (urlFileSize == localFileSize) return false;

            // Remove existing and create new directory
            Helper.RemoveDirectory(_downloadPath, true);

            Console.WriteLine($"[INFO] Downloading {Name} from {DownloadUrl}");

            // Download
            using var webClient = new WebClient();
            webClient.DownloadFile(new Uri(DownloadUrl), _downloadPathFile);

            // Extract if archive
            var ext = Path.GetExtension(_downloadPathFile).ToLower();
            if (ext == ".zip")
            {
                System.IO.Compression.ZipFile.ExtractToDirectory(_downloadPathFile, _downloadPath);
            }
            else if (ext == ".gz" || ext == ".tar")
            {
                using var stream = File.OpenRead(_downloadPathFile);
                using var reader = ReaderFactory.Open(stream);
                while (reader.MoveToNextEntry())
                {
                    if (!reader.Entry.IsDirectory)
                    {
                        reader.WriteEntryToDirectory(_downloadPath,
                            new ExtractionOptions { ExtractFullPath = true, Overwrite = true });
                    }
                }
            }

            Console.WriteLine($"[INFO] Downloaded {Name} successfully");

            if (IsReadyToRun() && _runtimeArguments != null)
            {
                Run(_runtimeArguments);
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to install {Name}: {ex.Message}");
            Helper.RemoveDirectory(_downloadPath);
            return false;
        }
    }

    public override bool IsConfigurable() => Configuration != null;
    public override bool IsInstallable() => true;

    protected virtual bool IsReadyToRun()
    {
        _skipRun = !_skipRun;
        return _skipRun;
    }

    protected override string? SetRunExecutable() => Helper.SearchExecutable(_downloadPath);

    public string? GetExecutablePath() => SetRunExecutable();

    private void GeneratePaths()
    {
        _downloadPath = Path.Join(Helper.GetAppBasePath(), Name);
        var appFileName = Helper.GetFileNameByUrl(DownloadUrl);
        _downloadPathFile = Path.Join(_downloadPath, appFileName);
    }
}
