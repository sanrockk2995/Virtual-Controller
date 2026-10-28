using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VirtualController.Services;
using Xunit;

namespace VirtualController.Tests
{
    public class UpdateCheckerTests
    {
        [Theory]
        [InlineData("v1.3.1", 1, 3, 1)]
        [InlineData("1.4.0", 1, 4, 0)]
        [InlineData("v2.0", 2, 0, 0)]
        [InlineData("v1.5.0-beta", 1, 5, 0)]
        [InlineData("1.4.1+9ad5e6b82311ce67f79c65d9535079eaea9a2f21", 1, 4, 1)]
        [InlineData("V3.1.2.4", 3, 1, 2)]
        public void UpdateChecker_ParseVersion_ValidInputs_ParsedCorrectly(
            string input, int expectedMajor, int expectedMinor, int expectedBuild)
        {
            var ver = UpdateChecker.ParseVersion(input);
            Assert.NotNull(ver);
            Assert.Equal(expectedMajor, ver.Major);
            Assert.Equal(expectedMinor, ver.Minor);
            Assert.Equal(expectedBuild, ver.Build);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("invalid-tag")]
        public void UpdateChecker_ParseVersion_InvalidInputs_ReturnsNull(string? input)
        {
            var ver = UpdateChecker.ParseVersion(input);
            Assert.Null(ver);
        }

        [Fact]
        public void UpdateChecker_ParseReleaseJson_WhenRemoteIsNewer_DetectsUpdate()
        {
            string sampleJson = @"
            {
                ""tag_name"": ""v1.4.0"",
                ""name"": ""VirtualController v1.4.0"",
                ""body"": ""### Changes\n- New feature added."",
                ""html_url"": ""https://github.com/sanrockk2995/Virtual-Controller/releases/tag/v1.4.0"",
                ""published_at"": ""2026-10-28T12:00:00Z"",
                ""assets"": [
                    {
                        ""name"": ""VirtualController-v1.4.0-win-x64.zip"",
                        ""browser_download_url"": ""https://github.com/sanrockk2995/Virtual-Controller/releases/download/v1.4.0/VirtualController-v1.4.0-win-x64.zip""
                    }
                ]
            }";

            var current = new Version(1, 3, 1);
            var info = UpdateChecker.ParseReleaseJson(sampleJson, current);

            Assert.True(info.IsUpdateAvailable);
            Assert.Equal(new Version(1, 4, 0), info.LatestVersion);
            Assert.Equal("v1.4.0", info.TagName);
            Assert.Equal("VirtualController v1.4.0", info.ReleaseName);
            Assert.Contains("New feature added.", info.ReleaseNotes);
            Assert.Equal("https://github.com/sanrockk2995/Virtual-Controller/releases/download/v1.4.0/VirtualController-v1.4.0-win-x64.zip", info.DownloadUrl);
        }

        [Fact]
        public void UpdateChecker_ParseReleaseJson_WhenRemoteIsSameOrOlder_NoUpdateAvailable()
        {
            string sampleJson = @"
            {
                ""tag_name"": ""v1.3.1"",
                ""name"": ""VirtualController v1.3.1"",
                ""body"": ""Bug fixes."",
                ""html_url"": ""https://github.com/sanrockk2995/Virtual-Controller/releases/tag/v1.3.1"",
                ""assets"": []
            }";

            var current = new Version(1, 3, 1);
            var info = UpdateChecker.ParseReleaseJson(sampleJson, current);

            Assert.False(info.IsUpdateAvailable);
            Assert.Equal(new Version(1, 3, 1), info.LatestVersion);
        }

        [Fact]
        public async Task UpdateChecker_WithMockHttp_ReturnsUpdateAvailable()
        {
            string json = @"
            {
                ""tag_name"": ""v2.0.0"",
                ""name"": ""VirtualController v2.0.0"",
                ""html_url"": ""https://github.com/sanrockk2995/Virtual-Controller/releases/tag/v2.0.0"",
                ""assets"": []
            }";

            var handler = new MockHttpMessageHandler(json, HttpStatusCode.OK);
            var client = new HttpClient(handler);
            var checker = new UpdateChecker(client);

            var info = await checker.CheckForUpdatesAsync(new Version(1, 3, 1));

            Assert.True(info.IsUpdateAvailable);
            Assert.Equal(new Version(2, 0, 0), info.LatestVersion);
            Assert.Null(info.ErrorMessage);
        }

        [Fact]
        public async Task UpdateChecker_WithHttpError_ReturnsGracefulError()
        {
            var handler = new MockHttpMessageHandler("Not Found", HttpStatusCode.NotFound);
            var client = new HttpClient(handler);
            var checker = new UpdateChecker(client);

            var info = await checker.CheckForUpdatesAsync(new Version(1, 3, 1));

            Assert.False(info.IsUpdateAvailable);
            Assert.NotNull(info.ErrorMessage);
            Assert.Contains("404", info.ErrorMessage);
        }

        [Fact]
        public void UpdateChecker_GetCurrentVersion_ReturnsValidNonEmptyVersion()
        {
            var ver = UpdateChecker.GetCurrentVersion();
            Assert.NotNull(ver);
            Assert.True(ver.Major >= 1);
        }

        [Fact]
        public void AppSettings_AutoCheckUpdates_SerializesCorrectly()
        {
            var settings = new AppSettings
            {
                AutoCheckUpdates = false
            };

            string json = JsonSerializer.Serialize(settings);
            var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

            Assert.NotNull(deserialized);
            Assert.False(deserialized.AutoCheckUpdates);
        }

        private class MockHttpMessageHandler : HttpMessageHandler
        {
            private readonly string _responseContent;
            private readonly HttpStatusCode _statusCode;

            public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode)
            {
                _responseContent = responseContent;
                _statusCode = statusCode;
            }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var response = new HttpResponseMessage(_statusCode)
                {
                    Content = new StringContent(_responseContent)
                };
                return Task.FromResult(response);
            }
        }
    }
}
