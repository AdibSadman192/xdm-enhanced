using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace XDM.Core.Network
{
    /// <summary>
    /// Monitors network conditions and bandwidth usage
    /// </summary>
    public class NetworkMonitor : IDisposable
    {
        private readonly CircularBuffer<NetworkSample> _samples;
        private readonly Timer _sampleTimer;
        private readonly NetworkInterface[] _activeInterfaces;
        private readonly Dictionary<string, long> _lastBytesReceived;
        private readonly object _lock = new object();
        private bool _disposed;

        public NetworkMonitor()
        {
            _samples = new CircularBuffer<NetworkSample>(1000); // Keep last 1000 samples
            _lastBytesReceived = new Dictionary<string, long>();
            _activeInterfaces = GetActiveInterfaces();
            InitializeBaseline();

            // Sample every second
            _sampleTimer = new Timer(
                _ => CollectSample(),
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)
            );
        }

        private NetworkInterface[] GetActiveInterfaces()
        {
            return Array.FindAll(NetworkInterface.GetAllNetworkInterfaces(), nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                (nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                 nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet));
        }

        private void InitializeBaseline()
        {
            foreach (var nic in _activeInterfaces)
            {
                var stats = nic.GetIPv4Statistics();
                _lastBytesReceived[nic.Id] = stats.BytesReceived;
            }
        }

        private void CollectSample()
        {
            try
            {
                var totalBytesPerSecond = 0L;
                var timestamp = DateTime.Now;

                foreach (var nic in _activeInterfaces)
                {
                    var stats = nic.GetIPv4Statistics();
                    var currentBytes = stats.BytesReceived;

                    if (_lastBytesReceived.TryGetValue(nic.Id, out var lastBytes))
                    {
                        var bytesPerSecond = currentBytes - lastBytes;
                        if (bytesPerSecond >= 0) // Avoid negative values on counter reset
                        {
                            totalBytesPerSecond += bytesPerSecond;
                        }
                    }

                    _lastBytesReceived[nic.Id] = currentBytes;
                }

                var sample = new NetworkSample
                {
                    Timestamp = timestamp,
                    BytesPerSecond = totalBytesPerSecond,
                    NetworkType = GetPrimaryNetworkType(),
                    SignalStrength = GetWifiSignalStrength()
                };

                lock (_lock)
                {
                    _samples.Add(sample);
                }
            }
            catch (Exception)
            {
                // Log error but continue monitoring
            }
        }

        private NetworkType GetPrimaryNetworkType()
        {
            foreach (var nic in _activeInterfaces)
            {
                switch (nic.NetworkInterfaceType)
                {
                    case NetworkInterfaceType.Wireless80211:
                        return NetworkType.WiFi;
                    case NetworkInterfaceType.Ethernet:
                        return NetworkType.Ethernet;
                    case NetworkInterfaceType.Wwan:
                        return NetworkType.Mobile;
                }
            }
            return NetworkType.Unknown;
        }

        private int? GetWifiSignalStrength()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "netsh",
                            Arguments = "wlan show interfaces",
                            UseShellExecute = false,
                            RedirectStandardOutput = true,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    foreach (var line in output.Split('\n'))
                    {
                        if (line.Contains("Signal"))
                        {
                            var signalStr = line.Split(':')[1].Trim().TrimEnd('%');
                            if (int.TryParse(signalStr, out var signal))
                            {
                                return signal;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore errors in signal strength detection
                }
            }
            return null;
        }

        public NetworkUsage GetCurrentUsage()
        {
            lock (_lock)
            {
                var recentSamples = _samples.GetLastN(10); // Last 10 seconds
                if (recentSamples.Count == 0)
                {
                    return new NetworkUsage
                    {
                        AverageBytesPerSecond = 0,
                        NetworkType = GetPrimaryNetworkType(),
                        SignalStrength = GetWifiSignalStrength()
                    };
                }

                var avgBytesPerSecond = recentSamples.Average(s => s.BytesPerSecond);
                return new NetworkUsage
                {
                    AverageBytesPerSecond = avgBytesPerSecond,
                    NetworkType = recentSamples[^1].NetworkType,
                    SignalStrength = recentSamples[^1].SignalStrength
                };
            }
        }

        public IReadOnlyList<NetworkSample> GetHistoricalData(TimeSpan duration)
        {
            var cutoff = DateTime.Now - duration;
            lock (_lock)
            {
                return _samples.Where(s => s.Timestamp >= cutoff).ToList();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _sampleTimer?.Dispose();
                _disposed = true;
            }
        }
    }

    public class NetworkSample
    {
        public DateTime Timestamp { get; set; }
        public long BytesPerSecond { get; set; }
        public NetworkType NetworkType { get; set; }
        public int? SignalStrength { get; set; }
    }

    public class NetworkUsage
    {
        public double AverageBytesPerSecond { get; set; }
        public NetworkType NetworkType { get; set; }
        public int? SignalStrength { get; set; }
    }

    public enum NetworkType
    {
        Unknown,
        Ethernet,
        WiFi,
        Mobile
    }

    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _start;
        private int _count;

        public CircularBuffer(int capacity)
        {
            _buffer = new T[capacity];
            _start = 0;
            _count = 0;
        }

        public void Add(T item)
        {
            var index = (_start + _count) % _buffer.Length;
            _buffer[index] = item;

            if (_count < _buffer.Length)
            {
                _count++;
            }
            else
            {
                _start = (_start + 1) % _buffer.Length;
            }
        }

        public List<T> GetLastN(int n)
        {
            var result = new List<T>();
            var actualN = Math.Min(n, _count);

            for (int i = _count - actualN; i < _count; i++)
            {
                var index = (_start + i) % _buffer.Length;
                result.Add(_buffer[index]);
            }

            return result;
        }

        public IEnumerable<T> Where(Func<T, bool> predicate)
        {
            for (int i = 0; i < _count; i++)
            {
                var index = (_start + i) % _buffer.Length;
                var item = _buffer[index];
                if (predicate(item))
                {
                    yield return item;
                }
            }
        }
    }
}
