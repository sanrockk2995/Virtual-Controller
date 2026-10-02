using System;
using System.IO;
using Microsoft.Win32;

namespace VirtualController.Backend
{
    public static class DriverDetector
    {
        public static bool IsViGEmBusInstalled()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\ViGEmBus");
                if (key != null)
                {
                    return true;
                }
            }
            catch
            {
                // Registry query failed or permission denied
            }

            string driverPath = Path.Combine(Environment.SystemDirectory, "drivers", "ViGEmBus.sys");
            if (File.Exists(driverPath))
            {
                return true;
            }

            return false;
        }

        public static bool CanConnect()
        {
            try
            {
                using var client = new Nefarius.ViGEm.Client.ViGEmClient();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string GetStatusReport()
        {
            if (CanConnect())
            {
                return "ViGEmBus driver is installed, running, and ready for virtual controller emulation.";
            }

            if (IsViGEmBusInstalled())
            {
                return "ViGEmBus driver files are present, but the driver service is currently stopped or inaccessible.";
            }

            return "ViGEmBus driver is NOT installed on this computer. Click below to install it automatically.";
        }
    }
}
