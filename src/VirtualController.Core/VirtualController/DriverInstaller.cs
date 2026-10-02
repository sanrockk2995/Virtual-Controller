using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using VirtualController.Services;

namespace VirtualController.Backend
{
    public static class DriverInstaller
    {
        public const string OfficialDownloadUrl = 
            "https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe";

        public static async Task<bool> InstallDriverAsync(IProgress<string>? progress = null)
        {
            // 1. If already connected, no installation required
            if (DriverDetector.CanConnect())
            {
                progress?.Report("ViGEmBus driver is already installed and operational.");
                return true;
            }

            // 2. If registered in registry, try starting service first
            if (DriverDetector.IsViGEmBusInstalled())
            {
                progress?.Report("ViGEmBus files exist. Starting driver service...");
                AppLogger.Info("ViGEmBus registered but not connected. Starting service...");
                try
                {
                    var startPsi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c net start ViGEmBus",
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using var startProc = Process.Start(startPsi);
                    if (startProc != null)
                    {
                        await startProc.WaitForExitAsync();
                        await Task.Delay(1000);
                        if (DriverDetector.CanConnect())
                        {
                            progress?.Report("ViGEmBus service started successfully!");
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Warning($"Could not start ViGEmBus service directly: {ex.Message}");
                }
            }

            // 3. Download official installer from GitHub release
            string installerPath = Path.Combine(Path.GetTempPath(), "ViGEmBus_1.22.0_x64_x86_arm64.exe");

            try
            {
                progress?.Report("Downloading official ViGEmBus installer from GitHub...");
                AppLogger.Info($"Downloading ViGEmBus installer from {OfficialDownloadUrl}...");

                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "VirtualXboxController-Installer");
                    using var response = await httpClient.GetAsync(OfficialDownloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();

                    using var streamToReadFrom = await response.Content.ReadAsStreamAsync();
                    using var streamToWriteTo = File.Open(installerPath, FileMode.Create);
                    await streamToReadFrom.CopyToAsync(streamToWriteTo);
                }

                progress?.Report("Running installer (Please accept Windows UAC elevation prompt)...");
                AppLogger.Info($"Launching installer: {installerPath}");

                var psi = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/passive /norestart",
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using var process = Process.Start(psi);
                if (process == null)
                {
                    progress?.Report("Failed to start installer process.");
                    AppLogger.Error("Process.Start returned null for ViGEmBus installer.");
                    return false;
                }

                await process.WaitForExitAsync();
                AppLogger.Info($"ViGEmBus installer completed with exit code: {process.ExitCode}");

                progress?.Report("Verifying ViGEmBus driver registration...");
                await Task.Delay(2000);

                // Check connection or start service
                try
                {
                    var startPsi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c net start ViGEmBus",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var startProc = Process.Start(startPsi);
                    if (startProc != null) await startProc.WaitForExitAsync();
                }
                catch { }

                bool operational = DriverDetector.CanConnect() || DriverDetector.IsViGEmBusInstalled();
                if (operational)
                {
                    progress?.Report("ViGEmBus driver successfully installed and operational!");
                    AppLogger.Info("ViGEmBus verified operational.");
                    
                    try { File.Delete(installerPath); } catch { }
                    return true;
                }
                else
                {
                    progress?.Report("Installation completed, but driver service was not detected.");
                    AppLogger.Warning("Installer completed but driver was not detected.");
                    return false;
                }
            }
            catch (System.ComponentModel.Win32Exception winEx) when (winEx.NativeErrorCode == 1223)
            {
                progress?.Report("Installation cancelled: Administrator permission was declined.");
                AppLogger.Warning("User declined UAC prompt during ViGEmBus installation.");
                return false;
            }
            catch (Exception ex)
            {
                progress?.Report($"Driver installation error: {ex.Message}");
                AppLogger.Error("Failed to install ViGEmBus driver automatically.", ex);
                return false;
            }
        }
    }
}
