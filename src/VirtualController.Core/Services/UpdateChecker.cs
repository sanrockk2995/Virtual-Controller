using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace VirtualController.Services
{
    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public Version CurrentVersion { get; set; } = new(1, 3, 1);
        public Version? LatestVersion { get; set; }
        public string TagName { get; set; } = "";
        public string ReleaseName { get; set; } = "";
        public string ReleaseNotes { get; set; } = "";
        public string HtmlUrl { get; set; } = "";
        public string? DownloadUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class UpdateChecker
    {
        public const string DefaultRepoOwner = "sanrockk2995";
        public const string DefaultRepoName = "Virtual-Controller";
        public static readonly Version CurrentAppVersion = new(1, 3, 1);

        private readonly HttpClient _httpClient;
        private readonly string _repoOwner;
        private readonly string _repoName;

        public UpdateChecker(HttpClient? httpClient = null, string repoOwner = DefaultRepoOwner, string repoName = DefaultRepoName)
        {
            _httpClient = httpClient ?? CreateDefaultHttpClient();
            _repoOwner = repoOwner;
            _repoName = repoName;
        }

        private static HttpClient CreateDefaultHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(8)
            };
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("VirtualController-Updater", "1.3.1"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            return client;
        }

        public static Version GetCurrentVersion()
        {
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var ver = asm.GetName().Version;
                if (ver != null && (ver.Major > 0 || ver.Minor > 0 || ver.Build > 0))
                {
                    return new Version(ver.Major, ver.Minor, Math.Max(0, ver.Build));
                }
            }
            catch
            {
                // Fall back
            }
            return CurrentAppVersion;
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync(Version? customCurrentVersion = null, CancellationToken cancellationToken = default)
        {
            var currentVersion = customCurrentVersion ?? GetCurrentVersion();
            var result = new UpdateInfo
            {
                CurrentVersion = currentVersion
            };

            try
            {
                string url = $"https://api.github.com/repos/{_repoOwner}/{_repoName}/releases/latest";
                using var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                {
                    result.ErrorMessage = $"GitHub API returned {(int)response.StatusCode} ({response.StatusCode})";
                    return result;
                }

                string json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return ParseReleaseJson(json, currentVersion);
            }
            catch (Exception ex)
            {
                AppLogger.Warning($"Check for updates failed: {ex.Message}");
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        public static UpdateInfo ParseReleaseJson(string json, Version currentVersion)
        {
            var result = new UpdateInfo
            {
                CurrentVersion = currentVersion
            };

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("tag_name", out var tagElem))
            {
                result.TagName = tagElem.GetString() ?? "";
            }

            if (root.TryGetProperty("name", out var nameElem))
            {
                result.ReleaseName = nameElem.GetString() ?? "";
            }

            if (root.TryGetProperty("body", out var bodyElem))
            {
                result.ReleaseNotes = bodyElem.GetString() ?? "";
            }

            if (root.TryGetProperty("html_url", out var htmlElem))
            {
                result.HtmlUrl = htmlElem.GetString() ?? "";
            }

            if (root.TryGetProperty("published_at", out var pubElem) && pubElem.TryGetDateTime(out var dt))
            {
                result.PublishedAt = dt;
            }

            // Find asset download url (.zip)
            if (root.TryGetProperty("assets", out var assetsElem) && assetsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsElem.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out var assetNameElem) &&
                        asset.TryGetProperty("browser_download_url", out var dlElem))
                    {
                        string? name = assetNameElem.GetString();
                        if (name != null && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            result.DownloadUrl = dlElem.GetString();
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(result.DownloadUrl))
            {
                result.DownloadUrl = result.HtmlUrl;
            }

            var latestVer = ParseVersion(result.TagName);
            result.LatestVersion = latestVer;

            if (latestVer != null && latestVer > currentVersion)
            {
                result.IsUpdateAvailable = true;
            }

            return result;
        }

        public static Version? ParseVersion(string? tag)
        {
            if (string.IsNullOrWhiteSpace(tag)) return null;

            string cleaned = tag.Trim().TrimStart('v', 'V');
            int dashIndex = cleaned.IndexOf('-');
            if (dashIndex > 0)
            {
                cleaned = cleaned.Substring(0, dashIndex);
            }

            var parts = cleaned.Split('.');
            if (parts.Length == 1 && int.TryParse(parts[0], out int major))
            {
                return new Version(major, 0, 0);
            }
            if (parts.Length == 2 && int.TryParse(parts[0], out major) && int.TryParse(parts[1], out int minor))
            {
                return new Version(major, minor, 0);
            }
            if (Version.TryParse(cleaned, out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }
}
