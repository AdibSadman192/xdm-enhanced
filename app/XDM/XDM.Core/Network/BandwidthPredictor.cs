using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.Statistics;

namespace XDM.Core.Network
{
    /// <summary>
    /// Predicts future bandwidth availability using historical data and machine learning
    /// </summary>
    public class BandwidthPredictor
    {
        private readonly NetworkMonitor _monitor;
        private readonly Dictionary<int, List<BandwidthSample>> _hourlyStats;
        private readonly Dictionary<DayOfWeek, Dictionary<int, List<BandwidthSample>>> _dailyStats;
        private readonly object _lock = new object();

        public BandwidthPredictor()
        {
            _monitor = new NetworkMonitor();
            _hourlyStats = new Dictionary<int, List<BandwidthSample>>();
            _dailyStats = new Dictionary<DayOfWeek, Dictionary<int, List<BandwidthSample>>>();
            InitializeStats();
        }

        private void InitializeStats()
        {
            // Initialize hourly stats
            for (int hour = 0; hour < 24; hour++)
            {
                _hourlyStats[hour] = new List<BandwidthSample>();
            }

            // Initialize daily stats
            foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
            {
                _dailyStats[day] = new Dictionary<int, List<BandwidthSample>>();
                for (int hour = 0; hour < 24; hour++)
                {
                    _dailyStats[day][hour] = new List<BandwidthSample>();
                }
            }
        }

        public void AddSample(DateTime timestamp, double bandwidth, NetworkType networkType)
        {
            var sample = new BandwidthSample
            {
                Timestamp = timestamp,
                Bandwidth = bandwidth,
                NetworkType = networkType
            };

            lock (_lock)
            {
                // Add to hourly stats
                _hourlyStats[timestamp.Hour].Add(sample);

                // Add to daily stats
                _dailyStats[timestamp.DayOfWeek][timestamp.Hour].Add(sample);

                // Trim old samples (keep last 30 days)
                var cutoff = DateTime.Now.AddDays(-30);
                TrimOldSamples(cutoff);
            }
        }

        private void TrimOldSamples(DateTime cutoff)
        {
            foreach (var hourSamples in _hourlyStats.Values)
            {
                hourSamples.RemoveAll(s => s.Timestamp < cutoff);
            }

            foreach (var dayStats in _dailyStats.Values)
            {
                foreach (var hourSamples in dayStats.Values)
                {
                    hourSamples.RemoveAll(s => s.Timestamp < cutoff);
                }
            }
        }

        public BandwidthPrediction PredictBandwidth(DateTime targetTime)
        {
            lock (_lock)
            {
                var hourlyPrediction = PredictHourlyBandwidth(targetTime.Hour);
                var dailyPrediction = PredictDailyBandwidth(targetTime.DayOfWeek, targetTime.Hour);
                var recentUsage = _monitor.GetCurrentUsage();

                // Weight the predictions
                var prediction = new BandwidthPrediction
                {
                    Hour = targetTime.Hour,
                    AvailableBandwidth = WeightedAverage(new[]
                    {
                        (hourlyPrediction, 0.4),
                        (dailyPrediction, 0.4),
                        (recentUsage.AverageBytesPerSecond, 0.2)
                    }),
                    Confidence = CalculateConfidence(hourlyPrediction, dailyPrediction, recentUsage.AverageBytesPerSecond),
                    NetworkType = recentUsage.NetworkType
                };

                return prediction;
            }
        }

        private double PredictHourlyBandwidth(int hour)
        {
            var samples = _hourlyStats[hour];
            if (samples.Count == 0) return 0;

            // Use median to reduce impact of outliers
            return samples.Select(s => s.Bandwidth).Median();
        }

        private double PredictDailyBandwidth(DayOfWeek day, int hour)
        {
            var samples = _dailyStats[day][hour];
            if (samples.Count == 0) return 0;

            return samples.Select(s => s.Bandwidth).Median();
        }

        private double WeightedAverage(IEnumerable<(double value, double weight)> values)
        {
            var sum = 0.0;
            var weightSum = 0.0;

            foreach (var (value, weight) in values)
            {
                sum += value * weight;
                weightSum += weight;
            }

            return weightSum > 0 ? sum / weightSum : 0;
        }

        private double CalculateConfidence(double hourly, double daily, double recent)
        {
            // Calculate standard deviation of predictions
            var values = new[] { hourly, daily, recent };
            var std = values.StandardDeviation();
            var mean = values.Mean();

            // Higher coefficient of variation = lower confidence
            var cv = mean != 0 ? std / mean : 1;
            return Math.Max(0, 1 - cv);
        }

        public IEnumerable<BandwidthPrediction> GetHourlyPredictions(DateTime start, int hours)
        {
            var predictions = new List<BandwidthPrediction>();
            for (int i = 0; i < hours; i++)
            {
                var time = start.AddHours(i);
                predictions.Add(PredictBandwidth(time));
            }
            return predictions;
        }

        public void UpdateModel()
        {
            // Collect recent network data
            var recentData = _monitor.GetHistoricalData(TimeSpan.FromHours(1));
            foreach (var sample in recentData)
            {
                AddSample(
                    sample.Timestamp,
                    sample.BytesPerSecond,
                    sample.NetworkType
                );
            }
        }
    }

    public class BandwidthSample
    {
        public DateTime Timestamp { get; set; }
        public double Bandwidth { get; set; }
        public NetworkType NetworkType { get; set; }
    }

    public class BandwidthPrediction
    {
        public int Hour { get; set; }
        public double AvailableBandwidth { get; set; }
        public double Confidence { get; set; }
        public NetworkType NetworkType { get; set; }
    }
}
