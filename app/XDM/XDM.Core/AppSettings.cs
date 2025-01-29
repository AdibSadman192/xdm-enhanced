using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace XDM.Core
{
    public class AppSettings
    {
        public string DownloadDirectory { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public int MaxConcurrentDownloads { get; set; } = 3;
        public long MaxBandwidthBytesPerSecond { get; set; } = 0; // 0 means unlimited
        public bool EnableSmartScheduling { get; set; } = true;
        public bool EnableCloudIntegration { get; set; } = false;
        public bool EnableVideoConversion { get; set; } = false;
        public bool EnableBrowserMonitoring { get; set; } = true;
        public bool EnableSecureStorage { get; set; } = true;
        public bool EnableAutoUpdate { get; set; } = true;
        public List<string> FileTypeFilters { get; set; } = new();
        public Dictionary<string, string> CustomHeaders { get; set; } = new();
        public ProxySettings ProxySettings { get; set; } = new();
        public NetworkSettings NetworkSettings { get; set; } = new();
        public SecuritySettings SecuritySettings { get; set; } = new();
        public UISettings UISettings { get; set; } = new();

        public static AppSettings Default => new();

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public static AppSettings FromJson(string json)
        {
            return JsonConvert.DeserializeObject<AppSettings>(json) ?? Default;
        }
    }

    public class ProxySettings
    {
        public bool EnableProxy { get; set; }
        public string ProxyHost { get; set; }
        public int ProxyPort { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public bool BypassProxyForLocalAddresses { get; set; } = true;
    }

    public class NetworkSettings
    {
        public bool EnableChunkedDownload { get; set; } = true;
        public int ChunkSize { get; set; } = 1024 * 1024; // 1MB
        public int ConnectionTimeout { get; set; } = 30; // seconds
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelay { get; set; } = 5; // seconds
        public bool EnableKeepAlive { get; set; } = true;
    }

    public class SecuritySettings
    {
        public bool ValidateSSLCertificates { get; set; } = true;
        public bool EnableAntimalware { get; set; } = true;
        public bool EncryptPasswords { get; set; } = true;
        public bool EnableFileIntegrityCheck { get; set; } = true;
    }

    public class UISettings
    {
        public string Theme { get; set; } = "Light";
        public bool ShowNotifications { get; set; } = true;
        public bool MinimizeToTray { get; set; } = true;
        public bool ShowSpeedInBytes { get; set; } = false;
        public bool ShowGridLines { get; set; } = true;
        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
    }
}
