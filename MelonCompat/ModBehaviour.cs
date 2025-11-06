using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Debug = UnityEngine.Debug;

namespace MelonCompat;

internal class ModBehaviour : Duckov.Modding.ModBehaviour
{
    private static readonly string GameBasePath = AppDomain.CurrentDomain.BaseDirectory;
    private static readonly string ModBasePath = Path.Combine(Path.GetDirectoryName(typeof(ModBehaviour).Assembly.Location));

    private static readonly Dictionary<string, string> MelonLoaderFileNames = new()
    {
        ["Windows"] = "MelonLoader.x64.zip",
        ["macOS"] = "MelonLoader.macOS.x64.zip",
        ["Linux"] = "MelonLoader.Linux.x64.zip"
    };

    private async void OnEnable()
    {
        await CheckMelonInstall();
        var source = Path.Combine(ModBasePath, "Bootstrap.dll");
        var destination = Path.Combine(GameBasePath, "Plugins", "Bootstrap.dll");
        var destinationDir = Path.GetDirectoryName(destination)!;
        if (!File.Exists(destination))
        {
            Directory.CreateDirectory(destinationDir);
            File.Copy(source, destination);
        }
    }

    private static async UniTask CheckMelonInstall()
    {
        var versionDllPath = Path.Combine(GameBasePath, "version.dll");
        if (File.Exists(versionDllPath))
        {
            var version = ReadFileVersion(versionDllPath);
            if (version == "0.7.1.0")
            {
                return;
            }
        }

        Debug.Log("MelonLoader not installed.");

        var platform = GetOSPlatform();
        if (!MelonLoaderFileNames.TryGetValue(platform, out var fileName))
        {
            Debug.LogError($"Unsupported OS: {platform}");
            return;
        }

        var melonLoaderZipPath = Path.Combine(ModBasePath, fileName);
        if (!File.Exists(melonLoaderZipPath))
        {
            Debug.LogError($"{fileName} is missing from Mod folder.");
            var downloadUrl = $"https://github.com/LavaGang/MelonLoader/releases/download/v0.7.1/{fileName}";
            try
            {
                using var client = new HttpClient();
                Debug.Log($"Downloading MelonLoader from {downloadUrl}...");

                using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var fs = new FileStream(melonLoaderZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                await using var stream = await response.Content.ReadAsStreamAsync();
                await stream.CopyToAsync(fs);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to download MelonLoader: {ex.Message}");
                return;
            }
        }

        ZipFile.ExtractToDirectory(melonLoaderZipPath, GameBasePath, true);
    }

    private static string ReadFileVersion(string filePath)
    {
        var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
        return versionInfo.FileVersion;
    }

    private static string GetOSPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macOS";
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }

        return "Unknown";
    }
}