using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PKHeX.Core;

public static class UpdateUtil
{
    /// <summary>
    /// Gets the latest version of PKHeX according to the GitHub API
    /// </summary>
    /// <returns>A version representing the latest available version of PKHeX, or null if the latest version could not be determined</returns>
    public static Version? GetLatestPKHeXVersion()
    {
        const string apiEndpoint = "https://api.github.com/repos/hexbyt3/PKHeX-ALL-IN-ONE/releases/latest";
        var responseJson = NetUtil.GetStringFromURL(new Uri(apiEndpoint));
        if (responseJson is null)
            return null;

        // Parse it manually; no need to parse the entire json to object.
        const string tag = "tag_name";
        var index = responseJson.IndexOf(tag, StringComparison.Ordinal);
        if (index == -1)
            return null;

        var first = responseJson.IndexOf('"', index + tag.Length + 1) + 1;
        if (first == 0)
            return null;
        var second = responseJson.IndexOf('"', first);
        if (second == -1)
            return null;

        var tagString = responseJson.AsSpan()[first..second];
        
        // Extract base version from tag (remove v prefix and -rev.X suffix)
        var versionStr = tagString.ToString();
        if (versionStr.StartsWith("v"))
            versionStr = versionStr.Substring(1);
            
        // Remove revision suffix if present
        var revIndex = versionStr.IndexOf("-rev.", StringComparison.Ordinal);
        if (revIndex != -1)
            versionStr = versionStr.Substring(0, revIndex);
            
        return !Version.TryParse(versionStr, out var latestVersion) ? null : latestVersion;
    }
    
    /// <summary>
    /// Gets the latest release tag including revision suffix
    /// </summary>
    /// <returns>The latest release tag, or null if not found</returns>
    public static string? GetLatestReleaseTag()
    {
        const string apiEndpoint = "https://api.github.com/repos/hexbyt3/PKHeX-ALL-IN-ONE/releases/latest";
        var responseJson = NetUtil.GetStringFromURL(new Uri(apiEndpoint));
        if (responseJson is null)
            return null;

        // Parse the tag_name field
        const string tag = "tag_name";
        var index = responseJson.IndexOf(tag, StringComparison.Ordinal);
        if (index == -1)
            return null;

        var first = responseJson.IndexOf('"', index + tag.Length + 1) + 1;
        if (first == 0)
            return null;
        var second = responseJson.IndexOf('"', first);
        if (second == -1)
            return null;

        return responseJson.Substring(first, second - first);
    }

    /// <summary>
    /// Gets the download URL for the latest PKHeX release
    /// </summary>
    /// <returns>The download URL for the latest release, or null if not found</returns>
    public static string? GetLatestDownloadUrl()
    {
        const string apiEndpoint = "https://api.github.com/repos/hexbyt3/PKHeX-ALL-IN-ONE/releases/latest";
        var responseJson = NetUtil.GetStringFromURL(new Uri(apiEndpoint));
        if (responseJson is null)
            return null;

        // Look for the browser_download_url for the PKHeX.exe file
        const string downloadUrlKey = "browser_download_url";
        var index = responseJson.IndexOf(downloadUrlKey, StringComparison.Ordinal);
        
        while (index != -1)
        {
            var first = responseJson.IndexOf('"', index + downloadUrlKey.Length + 1) + 1;
            if (first == 0)
                break;
            var second = responseJson.IndexOf('"', first);
            if (second == -1)
                break;

            var url = responseJson.Substring(first, second - first);
            
            // Look for PKHeX.exe specifically
            if (url.EndsWith("PKHeX.exe", StringComparison.OrdinalIgnoreCase))
                return url;
                
            // Try next occurrence
            index = responseJson.IndexOf(downloadUrlKey, second, StringComparison.Ordinal);
        }

        return null;
    }

    /// <summary>
    /// Downloads the update file with progress reporting
    /// </summary>
    public static async Task<string?> DownloadUpdateAsync(string downloadUrl, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
            
            // Get the response to check content length
            using var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            
            // Check if we're downloading .exe or .zip
            var isExe = downloadUrl.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            var tempFileName = isExe ? $"PKHeX_Update_{Guid.NewGuid()}.exe" : $"PKHeX_Update_{Guid.NewGuid()}.zip";
            var tempPath = Path.Combine(Path.GetTempPath(), tempFileName);
            
            await using var fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
            await using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            
            var buffer = new byte[8192];
            var totalBytesRead = 0L;
            int bytesRead;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, cancellationToken)) != 0)
            {
                await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytesRead += bytesRead;
                
                if (totalBytes > 0)
                {
                    var percentComplete = (int)((totalBytesRead * 100L) / totalBytes);
                    progress?.Report(percentComplete);
                }
            }
            
            return tempPath;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error downloading update: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Creates an update script that will replace the current executable after the application exits
    /// </summary>
    public static bool PrepareUpdateScript(string updateFilePath, string currentExePath)
    {
        try
        {
            var scriptPath = Path.Combine(Path.GetTempPath(), "PKHeX_Update.ps1");
            var isExe = updateFilePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
            
            string script;
            if (isExe)
            {
                // Simple .exe replacement script
                script = $@"
# Wait for PKHeX to close
Start-Sleep -Seconds 2

# Backup current executable
$backupDir = '{Path.GetDirectoryName(currentExePath)}\backup_' + (Get-Date -Format 'yyyyMMddHHmmss')
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
Copy-Item -Path '{currentExePath}' -Destination $backupDir -Force

# Replace the executable
Copy-Item -Path '{updateFilePath}' -Destination '{currentExePath}' -Force

# Clean up
Remove-Item -Path '{updateFilePath}' -Force

# Keep only the latest 3 backups
Get-ChildItem -Path '{Path.GetDirectoryName(currentExePath)}' -Directory -Filter 'backup_*' |
    Sort-Object -Property Name -Descending |
    Select-Object -Skip 3 |
    ForEach-Object {{ Remove-Item -Path $_.FullName -Recurse -Force }}

# Restart PKHeX
Start-Process -FilePath '{currentExePath}'

# Delete this script
Remove-Item -Path $MyInvocation.MyCommand.Path -Force
";
            }
            else
            {
                // ZIP extraction script (keeping for compatibility)
                var updateDir = Path.Combine(Path.GetDirectoryName(currentExePath)!, "update_temp");
                script = $@"
# Wait for PKHeX to close
Start-Sleep -Seconds 2

# Extract update to temp directory
$updateDir = '{updateDir}'
if (Test-Path $updateDir) {{
    Remove-Item -Path $updateDir -Recurse -Force
}}
New-Item -ItemType Directory -Path $updateDir -Force | Out-Null

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory('{updateFilePath}', $updateDir)

# Backup current installation
$backupDir = '{Path.GetDirectoryName(currentExePath)}\backup_' + (Get-Date -Format 'yyyyMMddHHmmss')
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null

# Copy current files to backup (excluding update_temp and backup directories)
Get-ChildItem -Path '{Path.GetDirectoryName(currentExePath)}' -Exclude 'update_temp', 'backup_*', 'cfg.json' | 
    ForEach-Object {{ Copy-Item -Path $_.FullName -Destination $backupDir -Recurse -Force }}

# Copy new files from update
Get-ChildItem -Path $updateDir -Recurse | 
    Where-Object {{ -not $_.PSIsContainer }} |
    ForEach-Object {{
        $targetPath = $_.FullName.Replace($updateDir, '{Path.GetDirectoryName(currentExePath)}')
        $targetDir = Split-Path -Parent $targetPath
        if (-not (Test-Path $targetDir)) {{
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }}
        Copy-Item -Path $_.FullName -Destination $targetPath -Force
    }}

# Clean up
Remove-Item -Path $updateDir -Recurse -Force
Remove-Item -Path '{updateFilePath}' -Force

# Keep only the latest 3 backups
Get-ChildItem -Path '{Path.GetDirectoryName(currentExePath)}' -Directory -Filter 'backup_*' |
    Sort-Object -Property Name -Descending |
    Select-Object -Skip 3 |
    ForEach-Object {{ Remove-Item -Path $_.FullName -Recurse -Force }}

# Restart PKHeX
Start-Process -FilePath '{currentExePath}'

# Delete this script
Remove-Item -Path $MyInvocation.MyCommand.Path -Force
";
            }
            
            File.WriteAllText(scriptPath, script);
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error preparing update script: {ex}");
            return false;
        }
    }

    /// <summary>
    /// Executes the update script and exits the application
    /// </summary>
    public static void ExecuteUpdate(string currentExePath)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), "PKHeX_Update.ps1");
        if (!File.Exists(scriptPath))
            return;

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-ExecutionPolicy Bypass -WindowStyle Hidden -File \"{scriptPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process.Start(startInfo);
        Environment.Exit(0);
    }
}
