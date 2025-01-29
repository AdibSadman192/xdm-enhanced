using System;
using System.Threading.Tasks;
using XDM.Core.Network;
using XDM.Core.Security;
using System.IO;
using System.Text.Json;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;

namespace XDM.Diagnostics
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--diagnose")
            {
                await RunDiagnostics();
            }
            else
            {
                Console.WriteLine("Usage: dotnet run --diagnose");
            }
        }

        static async Task RunDiagnostics()
        {
            Console.WriteLine("XDM Diagnostics Tool");
            Console.WriteLine("===================\n");

            try
            {
                // System Checks
                await CheckSystem();

                // Network Checks
                await CheckNetwork();

                // Security Checks
                await CheckSecurity();

                // File System Checks
                await CheckFileSystem();

                // Configuration Checks
                await CheckConfiguration();

                Console.WriteLine("\nDiagnostics completed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError during diagnostics: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static async Task CheckSystem()
        {
            Console.WriteLine("System Checks:");
            Console.WriteLine("-------------");

            // Check .NET version
            Console.WriteLine($"- .NET Version: {Environment.Version}");

            // Check OS
            Console.WriteLine($"- OS: {Environment.OSVersion}");

            // Check available memory
            var memory = GC.GetTotalMemory(false) / (1024 * 1024);
            Console.WriteLine($"- Available Memory: {memory}MB");

            // Check processor count
            Console.WriteLine($"- Processor Count: {Environment.ProcessorCount}");

            await Task.CompletedTask;
        }

        static async Task CheckNetwork()
        {
            Console.WriteLine("\nNetwork Checks:");
            Console.WriteLine("---------------");

            // Check network connectivity
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                Console.WriteLine("- Network Available: Yes");

                // Check active network interfaces
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus == OperationalStatus.Up)
                    {
                        Console.WriteLine($"- Active Interface: {ni.Name} ({ni.NetworkInterfaceType})");
                        var stats = ni.GetIPv4Statistics();
                        Console.WriteLine($"  * Speed: {ni.Speed / 1_000_000}Mbps");
                        Console.WriteLine($"  * Bytes Sent: {stats.BytesSent}");
                        Console.WriteLine($"  * Bytes Received: {stats.BytesReceived}");
                    }
                }
            }
            else
            {
                Console.WriteLine("- Network Available: No");
            }

            // Test bandwidth monitoring
            using (var monitor = new NetworkMonitor())
            {
                var usage = monitor.GetCurrentUsage();
                Console.WriteLine($"- Current Network Usage: {usage.AverageBytesPerSecond / 1024:F2}KB/s");
                Console.WriteLine($"- Network Type: {usage.NetworkType}");
                if (usage.SignalStrength.HasValue)
                {
                    Console.WriteLine($"- Signal Strength: {usage.SignalStrength}%");
                }
            }

            await Task.CompletedTask;
        }

        static async Task CheckSecurity()
        {
            Console.WriteLine("\nSecurity Checks:");
            Console.WriteLine("----------------");

            // Check HTTPS certificate store
            using (var store = new X509Store(StoreName.Root, StoreLocation.CurrentUser))
            {
                store.Open(OpenFlags.ReadOnly);
                Console.WriteLine($"- Root Certificates: {store.Certificates.Count}");
            }

            // Check secure storage
            try
            {
                var testData = "Test secure storage";
                var encrypted = SecureStorage.ProtectData(testData);
                var decrypted = SecureStorage.UnprotectData(encrypted);
                Console.WriteLine("- Secure Storage: Working");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"- Secure Storage: Error - {ex.Message}");
            }

            await Task.CompletedTask;
        }

        static async Task CheckFileSystem()
        {
            Console.WriteLine("\nFile System Checks:");
            Console.WriteLine("-------------------");

            // Check app data directory
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XDM"
            );

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            Console.WriteLine($"- App Data Path: {appDataPath}");

            // Check downloads directory
            var downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads",
                "XDM"
            );

            if (!Directory.Exists(downloadsPath))
            {
                Directory.CreateDirectory(downloadsPath);
            }

            Console.WriteLine($"- Downloads Path: {downloadsPath}");

            // Check disk space
            var drive = new DriveInfo(Path.GetPathRoot(downloadsPath));
            var freeSpace = drive.AvailableFreeSpace / (1024 * 1024 * 1024);
            Console.WriteLine($"- Free Disk Space: {freeSpace}GB");

            await Task.CompletedTask;
        }

        static async Task CheckConfiguration()
        {
            Console.WriteLine("\nConfiguration Checks:");
            Console.WriteLine("---------------------");

            var configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XDM",
                "settings.json"
            );

            if (File.Exists(configPath))
            {
                try
                {
                    var config = JsonSerializer.Deserialize<JsonDocument>(
                        await File.ReadAllTextAsync(configPath)
                    );
                    Console.WriteLine("- Configuration File: Valid JSON");
                }
                catch (JsonException)
                {
                    Console.WriteLine("- Configuration File: Invalid JSON");
                }
            }
            else
            {
                Console.WriteLine("- Configuration File: Not found");
            }

            // Check browser extension
            var extensionPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Google",
                "Chrome",
                "User Data",
                "Default",
                "Extensions"
            );

            if (Directory.Exists(extensionPath))
            {
                Console.WriteLine("- Chrome Extensions Directory: Found");
            }
            else
            {
                Console.WriteLine("- Chrome Extensions Directory: Not found");
            }
        }
    }
}
