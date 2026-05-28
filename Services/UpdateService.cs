using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

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

        var result = MessageBox.Show(
            owner,
            "A new QuickTools update is available. Install it now?",
            "QuickTools update",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        await DownloadAndInstallAsync(asset.BrowserDownloadUrl);
    }

    private static bool IsPublishedExecutable()
    {
        var processPath = Environment.ProcessPath;
        return !string.IsNullOrWhiteSpace(processPath)
            && Path.GetFileName(processPath).Equals("QuickTools.exe", StringComparison.OrdinalIgnoreCase);
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
        var payloadPath = Directory.GetDirectories(extractPath).FirstOrDefault() ?? extractPath;
        var currentProcessId = Environment.ProcessId;

        var script = $$"""
        $ErrorActionPreference = 'Stop'
        $processId = {{currentProcessId}}
        $payload = '{{EscapePowerShellPath(payloadPath)}}'
        $target = '{{EscapePowerShellPath(installDirectory)}}'
        $exe = Join-Path $target 'QuickTools.exe'

        try {
            Wait-Process -Id $processId -Timeout 30
        } catch {
            Stop-Process -Id $processId -Force -ErrorAction SilentlyContinue
        }

        Copy-Item -Path (Join-Path $payload '*') -Destination $target -Recurse -Force
        Start-Process -FilePath $exe
        """;

        await File.WriteAllTextAsync(scriptPath, script);

        Process.Start(new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\"")
        {
            UseShellExecute = true,
            WindowStyle = ProcessWindowStyle.Hidden
        });

        Application.Current.Dispatcher.Invoke(() => Application.Current.Shutdown());
    }

    private static string EscapePowerShellPath(string path)
    {
        return path.Replace("'", "''");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class GitHubRelease
    {
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
