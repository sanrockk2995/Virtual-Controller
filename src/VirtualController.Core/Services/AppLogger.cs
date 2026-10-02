using System;
using System.IO;

namespace VirtualController.Services
{
    public static class AppLogger
    {
        private static readonly object _fileLock = new();
        private static string _logDirectory = "logs";
        private static string _logFilePath = Path.Combine("logs", "application.log");

        static AppLogger()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                _logDirectory = Path.Combine(baseDir, "logs");
                _logFilePath = Path.Combine(_logDirectory, "application.log");

                if (!Directory.Exists(_logDirectory))
                {
                    Directory.CreateDirectory(_logDirectory);
                }
            }
            catch
            {
                // Fallback to local relative
            }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Warning(string message)
        {
            Write("WARN", message);
        }

        public static void Error(string message, Exception? ex = null)
        {
            string fullMsg = ex != null ? $"{message} | Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}" : message;
            Write("ERROR", fullMsg);
        }

        private static void Write(string level, string message)
        {
            string logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";

            // Console output for development/debugging
            Console.WriteLine(logLine);

            lock (_fileLock)
            {
                try
                {
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }
                    File.AppendAllText(_logFilePath, logLine + Environment.NewLine);
                }
                catch
                {
                    // Do not crash application if logging fails
                }
            }
        }
    }
}
