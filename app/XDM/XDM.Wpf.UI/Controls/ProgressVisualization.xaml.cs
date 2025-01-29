using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Configurations;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace XDM.Wpf.UI.Controls
{
    public partial class ProgressVisualization : UserControl, INotifyPropertyChanged
    {
        private double _progress;
        private string _speed;
        private string _timeRemaining;
        private ChartValues<double> _speedHistory;
        private string[] _timeLabels;
        private readonly int _maxDataPoints = 30;

        public ProgressVisualization()
        {
            InitializeComponent();
            DataContext = this;

            SpeedHistory = new ChartValues<double>();
            TimeLabels = new string[_maxDataPoints];
            SpeedFormatter = value => $"{value:F2} MB/s";

            // Initialize time labels
            for (int i = 0; i < _maxDataPoints; i++)
            {
                TimeLabels[i] = DateTime.Now.AddSeconds(-(_maxDataPoints - i)).ToString("HH:mm:ss");
            }
        }

        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Speed
        {
            get => _speed;
            set
            {
                if (_speed != value)
                {
                    _speed = value;
                    OnPropertyChanged();
                }
            }
        }

        public string TimeRemaining
        {
            get => _timeRemaining;
            set
            {
                if (_timeRemaining != value)
                {
                    _timeRemaining = value;
                    OnPropertyChanged();
                }
            }
        }

        public ChartValues<double> SpeedHistory
        {
            get => _speedHistory;
            set
            {
                if (_speedHistory != value)
                {
                    _speedHistory = value;
                    OnPropertyChanged();
                }
            }
        }

        public string[] TimeLabels
        {
            get => _timeLabels;
            set
            {
                if (_timeLabels != value)
                {
                    _timeLabels = value;
                    OnPropertyChanged();
                }
            }
        }

        public Func<double, string> SpeedFormatter { get; set; }

        public void UpdateProgress(double progress, double speedMBps, TimeSpan remaining)
        {
            Progress = progress;
            Speed = $"{speedMBps:F2} MB/s";
            TimeRemaining = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";

            // Update speed history
            if (SpeedHistory.Count >= _maxDataPoints)
            {
                SpeedHistory.RemoveAt(0);
            }
            SpeedHistory.Add(speedMBps);

            // Update time labels
            var currentTime = DateTime.Now;
            for (int i = 0; i < _maxDataPoints; i++)
            {
                TimeLabels[i] = currentTime.AddSeconds(-(_maxDataPoints - i)).ToString("HH:mm:ss");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
