using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Text.Json;
using System.Windows.Media;

namespace XDM.Wpf.UI.Themes
{
    /// <summary>
    /// Manages application themes including custom user themes
    /// </summary>
    public class ThemeManager
    {
        private readonly string _customThemesPath;
        private readonly Dictionary<string, ResourceDictionary> _loadedThemes;

        public ThemeManager()
        {
            _customThemesPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XDM",
                "Themes"
            );
            _loadedThemes = new Dictionary<string, ResourceDictionary>();

            // Ensure themes directory exists
            Directory.CreateDirectory(_customThemesPath);

            // Load built-in themes
            LoadBuiltInThemes();
        }

        private void LoadBuiltInThemes()
        {
            // Load dark theme
            _loadedThemes["Dark"] = new ResourceDictionary
            {
                Source = new Uri("/XDM.Wpf.UI;component/Themes/Dark.xaml", UriKind.Relative)
            };

            // Load light theme
            _loadedThemes["Light"] = new ResourceDictionary
            {
                Source = new Uri("/XDM.Wpf.UI;component/Themes/Light.xaml", UriKind.Relative)
            };
        }

        public void ApplyTheme(string themeName)
        {
            if (!_loadedThemes.ContainsKey(themeName))
            {
                LoadCustomTheme(themeName);
            }

            if (_loadedThemes.TryGetValue(themeName, out var theme))
            {
                Application.Current.Resources.MergedDictionaries.Clear();
                Application.Current.Resources.MergedDictionaries.Add(theme);
            }
            else
            {
                throw new ArgumentException($"Theme '{themeName}' not found");
            }
        }

        public void CreateCustomTheme(string name, ThemeDefinition definition)
        {
            var theme = new ResourceDictionary();

            // Convert color strings to brushes
            foreach (var (key, value) in definition.Colors)
            {
                if (TryParseColor(value, out var color))
                {
                    var brush = new SolidColorBrush(color);
                    brush.Freeze();
                    theme[key] = brush;
                }
            }

            // Add other theme resources
            foreach (var (key, value) in definition.Resources)
            {
                theme[key] = value;
            }

            // Save theme
            _loadedThemes[name] = theme;
            SaveCustomTheme(name, definition);
        }

        private void LoadCustomTheme(string name)
        {
            var themePath = Path.Combine(_customThemesPath, $"{name}.json");
            if (File.Exists(themePath))
            {
                var json = File.ReadAllText(themePath);
                var definition = JsonSerializer.Deserialize<ThemeDefinition>(json);
                CreateCustomTheme(name, definition);
            }
        }

        private void SaveCustomTheme(string name, ThemeDefinition definition)
        {
            var themePath = Path.Combine(_customThemesPath, $"{name}.json");
            var json = JsonSerializer.Serialize(definition, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(themePath, json);
        }

        private bool TryParseColor(string colorString, out Color color)
        {
            try
            {
                if (colorString.StartsWith("#"))
                {
                    var argb = Convert.ToUInt32(colorString.TrimStart('#'), 16);
                    color = Color.FromArgb(
                        (byte)((argb >> 24) & 0xFF),
                        (byte)((argb >> 16) & 0xFF),
                        (byte)((argb >> 8) & 0xFF),
                        (byte)(argb & 0xFF)
                    );
                    return true;
                }
                else
                {
                    var converter = new BrushConverter();
                    if (converter.ConvertFromString(colorString) is SolidColorBrush brush)
                    {
                        color = brush.Color;
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            color = Colors.Black;
            return false;
        }

        public IEnumerable<string> GetAvailableThemes()
        {
            var themes = new List<string>(_loadedThemes.Keys);
            
            // Add custom themes from disk
            var customThemes = Directory.GetFiles(_customThemesPath, "*.json")
                .Select(path => Path.GetFileNameWithoutExtension(path));
            themes.AddRange(customThemes);

            return themes;
        }

        public void DeleteCustomTheme(string name)
        {
            var themePath = Path.Combine(_customThemesPath, $"{name}.json");
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
                _loadedThemes.Remove(name);
            }
        }
    }

    public class ThemeDefinition
    {
        public Dictionary<string, string> Colors { get; set; } = new();
        public Dictionary<string, object> Resources { get; set; } = new();
    }
}
