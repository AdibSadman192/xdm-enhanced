using System.Collections.ObjectModel;
using System.Windows.Input;
using XDM.Core;

namespace XDM.Wpf.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the main window providing download management functionality
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private string _statusText = string.Empty;
        private bool _isDarkMode;
        private ObservableCollection<DownloadItem> _downloads;

        public MainWindowViewModel()
        {
            _downloads = new ObservableCollection<DownloadItem>();
            InitializeCommands();
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    ApplyTheme(value);
                }
            }
        }

        public ObservableCollection<DownloadItem> Downloads
        {
            get => _downloads;
            set => SetProperty(ref _downloads, value);
        }

        private void InitializeCommands()
        {
            // Commands will be initialized here
        }

        private void ApplyTheme(bool isDark)
        {
            // Theme application logic will be implemented here
            var theme = isDark ? "Dark" : "Light";
            App.Current.Resources.MergedDictionaries[0].Source = 
                new System.Uri($"/Themes/{theme}.xaml", System.UriKind.Relative);
        }
    }
}
