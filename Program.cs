using System;
using System.IO;
using Microsoft.Win32;
using System.Text;
using System.Linq;
using System.Runtime.Versioning;
using System.Runtime.InteropServices;

class LogMonitor
{
    static void Main()
    {
        var cfInstallDir = GetColdFusionInstallPath();

        if (string.IsNullOrEmpty(cfInstallDir) || !Directory.Exists(cfInstallDir))
        {
            Console.WriteLine("ColdFusion installation directory not found. Exiting...");
            return;
        }

        string logDirectory = Path.Combine(cfInstallDir, "cfusion", "logs");

        if (!Directory.Exists(logDirectory))
        {
            Console.WriteLine("Log directory not found. Exiting...");
            return;
        }

        string[] logFiles = Directory.GetFiles(logDirectory, "*.log");

        if (logFiles.Length == 0)
        {
            Console.WriteLine("No log files found. Exiting...");
            return;
        }

        // Clear the screen before redrawing the options
        static void ClearScreen()
        {
            Console.Clear();
        }

        Console.WriteLine("Available log files:");
        ClearScreen();
        for (int i = 0; i < logFiles.Length; i++)
        {
            Console.WriteLine($"{i + 1}: {Path.GetFileName(logFiles[i])}");
        }

        string? input;
        int fileIndex;

        while (true)
        {
            Console.WriteLine("Enter the number of the log file you want to monitor (or 'Q' to quit):");
            input = Console.ReadLine()?.Trim();

            if(input != null)
            {
                if (input.Equals("Q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Quitting program...");
                    return;
                }
            }

            if (int.TryParse(input, out fileIndex) && fileIndex >= 1 && fileIndex <= logFiles.Length)
            {
                break;
            }

            ClearScreen();
            Console.WriteLine("Available log files:");
            for (int i = 0; i < logFiles.Length; i++)
            {
                Console.WriteLine($"{i + 1}: {Path.GetFileName(logFiles[i])}");
            }
            Console.WriteLine("Invalid selection. Please try again.");
        }

        string filePath = logFiles[fileIndex - 1];

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            Console.TreatControlCAsInput = true;

            // Before the monitoring loop:
            Console.WriteLine($"{DateTime.Now}: Monitoring started");
            Console.Title = $"Monitoring: {Path.GetFileName(filePath)}";

            long lastMaxOffset = new FileInfo(filePath).Length;

            // Within the monitoring loop:
            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);
                    if (key.Modifiers == ConsoleModifiers.Control && (key.Key == ConsoleKey.C || key.Key == ConsoleKey.X || key.Key == ConsoleKey.Z))
                    {
                        Console.WriteLine("\nMonitoring stopped. Press any key to exit...");
                        Console.ReadKey(true);
                        return;
                    }
                }

                fs.Seek(lastMaxOffset, SeekOrigin.Begin);

                using (var sr = new StreamReader(fs, Encoding.UTF8, true, 4096, true))
                {
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                    }

                    lastMaxOffset = fs.Position;
                }

                System.Threading.Thread.Sleep(1000); // Wait for 1 second before checking for updates
            }

        }

    }

    static string? GetColdFusionInstallPath()
    {
        string? cfInstallPath = null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows-specific registry logic
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Adobe\Install Data\Adobe ColdFusion 2018");
                if (key != null)
                {
                    cfInstallPath = key.GetValue("CFMXRoot")?.ToString();
                    if (!string.IsNullOrEmpty(cfInstallPath))
                    {
                        cfInstallPath = Path.GetFullPath(Path.Combine(cfInstallPath, ".."));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving ColdFusion install path: {ex.Message}");
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux-specific logic
            string[] possiblePaths = [
                "/opt/coldfusion2018",
                "/opt/coldfusion2021",
                "/opt/coldfusion2023"
                // Add more possible installation paths here
            ];

            cfInstallPath = possiblePaths.FirstOrDefault(Directory.Exists);

            if (!string.IsNullOrEmpty(cfInstallPath))
            {
                cfInstallPath = Path.GetFullPath(Path.Combine(cfInstallPath, "cfusion"));
            }
        }

        return cfInstallPath;

    }
}