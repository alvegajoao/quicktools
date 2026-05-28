using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using QuickTools.Views;

namespace QuickTools.Services;

public sealed class UpdateService
{
    private const string ReleasesApiUrl = "https://api.github.com/repos/alvegajoao/quicktools/releases/tags/latest";
    private const string ReleaseAssetName = "QuickTools-win-x64.zip";
    private static readonly HttpClient HttpClient = new();

    public async Task CheckForUpdatesAsync(Window owner)
    {
        if (!IsPublishedExecutable())
        {
            return;
        }

        var currentCommit = GetCurrentCommit();
        if (string.IsNullOrWhiteSpace(currentCommit))
        {
            return;
        }

        var release = await GetLatestReleaseAsync();
        if (release is null || string.IsNullOrWhiteSpace(release.TargetCommitish))
        {
            return;
        }

        if (release.TargetCommitish.StartsWith(currentCommit, StringComparison.OrdinalIgnoreCase)
            || currentCommit.StartsWith(release.TargetCommitish, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var asset = release.Assets.FirstOrDefault(item => item.Name.Equals(ReleaseAssetName, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            return;
        }

        var shouldOpenGitHub = UpdateDialog.ShowConfirmation(
            owner,
            "QuickTools update",
            "A new QuickTools update is available. Please download it from GitHub.",
            "Open GitHub",
            "Later");

        if (!shouldOpenGitHub)
        {
            return;
        }

        try
        {
            OpenReleasePage(release.HtmlUrl, asset.BrowserDownloadUrl);
        }
        catch (Exception ex)
        {
            UpdateDialog.ShowError(owner, "QuickTools update", $"QuickTools could not open the GitHub release page.\n\n{ex.Message}");
        }
    }

    private static bool IsPublishedExecutable()
    {
        var processPath = Environment.ProcessPath;
        return !string.IsNullOrWhiteSpace(processPath)
            && Path.GetFileName(processPath).Equals("QuickTools.exe", StringComparison.OrdinalIgnoreCase);
    }

    private static void OpenReleasePage(string? releaseUrl, string? fallbackUrl)
    {
        var url = !string.IsNullOrWhiteSpace(releaseUrl)
            ? releaseUrl
            : fallbackUrl;

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Could not determine the GitHub release page.");
        }

        Process.Start(new ProcessStartInfo(url)
        {
            UseShellExecute = true
        });
    }

    private static string GetCurrentCommit()
    {
        var informationalVersion = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "";

        var plusIndex = informationalVersion.LastIndexOf('+');
        return plusIndex >= 0 && plusIndex < informationalVersion.Length - 1
            ? informationalVersion[(plusIndex + 1)..]
            : "";
    }

    private static async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ReleasesApiUrl);
        request.Headers.UserAgent.ParseAdd("QuickTools-Updater");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await HttpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonSerializer.DeserializeAsync<GitHubRelease>(stream, JsonOptions);
    }

    private static async Task DownloadAndInstallAsync(string url)
    {
        var processPath = Environment.ProcessPath ?? throw new InvalidOperationException("Could not locate QuickTools.exe.");
        var installDirectory = Path.GetDirectoryName(processPath) ?? throw new InvalidOperationException("Could not locate install directory.");
        var tempRoot = Path.Combine(Path.GetTempPath(), "QuickToolsUpdate", Guid.NewGuid().ToString("N"));
        var zipPath = Path.Combine(tempRoot, ReleaseAssetName);
        var extractPath = Path.Combine(tempRoot, "extract");
        var scriptPath = Path.Combine(tempRoot, "install-update.ps1");

        Directory.CreateDirectory(tempRoot);
        Directory.CreateDirectory(extractPath);

        await using (var download = await HttpClient.GetStreamAsync(url))
        await using (var file = File.Create(zipPath))
        {
            await download.CopyToAsync(file);
        }

        ZipFile.ExtractToDirectory(zipPath, extractPath, overwriteFiles: true);
        var payloadPath = GetPayloadPath(extractPath);
        Directory.CreateDirectory(installDirectory);
        var currentProcessId = Environment.ProcessId;

        var script = $$"""
        $ErrorActionPreference = 'Stop'
        Add-Type -AssemblyName PresentationFramework
        $processId = {{currentProcessId}}
        $payload = '{{EscapePowerShellPath(payloadPath)}}'
        $target = '{{EscapePowerShellPath(installDirectory)}}'
        $exe = Join-Path $target 'QuickTools.exe'

        try {
            try {
                Wait-Process -Id $processId -Timeout 30
            } catch {
                Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
            }

            New-Item -ItemType Directory -Path $target -Force | Out-Null
            Get-ChildItem -LiteralPath $payload -Force | Copy-Item -Destination $target -Recurse -Force
            Start-Process -FilePath $exe
        } catch {
            [System.Windows.MessageBox]::Show(
                "QuickTools could not finish installing the update.`n`n$($_.Exception.Message)",
                "QuickTools update",
                [System.Windows.MessageBoxButton]::OK,
                [System.Windows.MessageBoxImage]::Error) | Out-Null
        }
        """;

        await File.WriteAllTextAsync(scriptPath, script);

        Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"")
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
    }

    private static string EscapePowerShellPath(string path)
    {
        return path.Replace("'", "''");
    }

    private static string GetPayloadPath(string extractPath)
    {
        var publishedExe = Directory
            .EnumerateFiles(extractPath, "QuickTools.exe", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(publishedExe))
        {
            return Path.GetDirectoryName(publishedExe) ?? extractPath;
        }

        var rootFiles = Directory.GetFiles(extractPath);
        if (rootFiles.Length > 0)
        {
            return extractPath;
        }

        var rootDirectories = Directory.GetDirectories(extractPath);
        return rootDirectories.Length == 1
            ? rootDirectories[0]
            : extractPath;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GitHubRelease
    {
        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = "";

        [JsonPropertyName("target_commitish")]
        public string TargetCommitish { get; set; } = "";

        [JsonPropertyName("assets")]
        public List<GitHubAsset> Assets { get; set; } = [];
    }

    private sealed class GitHubAsset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("browser_download_url")]
        public string BrowserDownloadUrl { get; set; } = "";
    }
}
