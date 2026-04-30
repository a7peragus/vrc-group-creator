using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Media;
using VrcGroupCreator.Services;
using VrcGroupCreator.Models;

namespace VrcGroupCreator.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly ThemeService _themeService;
    private string _originalColorHex = "#673AB7";

    [ObservableProperty] private Color _primaryColor;
    
    [ObservableProperty] private double _hue;
    [ObservableProperty] private double _saturation;
    [ObservableProperty] private double _luminance;
    [ObservableProperty] private bool _enableDebugConsole;
    [ObservableProperty] private bool _enableFileLogging;

    private bool _isUpdating;

    public SettingsViewModel(SettingsService settingsService, ThemeService themeService)
    {
        _settingsService = settingsService;
        _themeService = themeService;
        
        EnableDebugConsole = _settingsService.Settings.EnableDebugConsole;
        EnableFileLogging = _settingsService.Settings.EnableFileLogging;
        LoadColors();
    }

    private void LoadColors()
    {
        var colors = _settingsService.Settings.Colors;
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colors.PrimaryColor);
            _originalColorHex = colors.PrimaryColor; // snapshot before any edits
            UpdateColorAndHsl(color, true);
        }
        catch
        {
            _originalColorHex = "#673AB7";
            UpdateColorAndHsl((Color)ColorConverter.ConvertFromString("#673AB7"), true);
        }
    }

    /// <summary>Restore the color that was active when the window opened and re-apply the theme.</summary>
    public void Revert()
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(_originalColorHex);
            UpdateColorAndHsl(color, true);
        }
        catch
        {
            UpdateColorAndHsl((Color)ColorConverter.ConvertFromString("#673AB7"), true);
        }
        // Re-apply original theme so live preview is undone
        var original = new ColorSettings { PrimaryColor = _originalColorHex };
        _themeService.ApplyTheme(original);
    }

    [RelayCommand]
    private void Save()
    {
        var settings = _settingsService.Settings;
        var colors = settings.Colors;
        colors.PrimaryColor = PrimaryColor.ToString();
        settings.EnableDebugConsole = EnableDebugConsole;
        settings.EnableFileLogging = EnableFileLogging;

        _settingsService.Save();
        _themeService.ApplyTheme(colors);
    }

    [RelayCommand]
    private void Reset()
    {
        UpdateColorAndHsl((Color)ColorConverter.ConvertFromString("#673AB7"), true);
        ApplyPreview();
    }

    partial void OnPrimaryColorChanged(Color value)
    {
        if (!_isUpdating)
        {
            UpdateColorAndHsl(value, false);
        }
        ApplyPreview();
    }

    partial void OnHueChanged(double value) => SyncColorFromHsl();
    partial void OnSaturationChanged(double value) => SyncColorFromHsl();
    partial void OnLuminanceChanged(double value) => SyncColorFromHsl();
    partial void OnEnableDebugConsoleChanged(bool value)
    {
        _settingsService.Settings.EnableDebugConsole = value;
        _settingsService.Save();
        LoggingService.SetConsoleEnabled(value);
    }

    partial void OnEnableFileLoggingChanged(bool value)
    {
        _settingsService.Settings.EnableFileLogging = value;
        _settingsService.Save();
        LoggingService.SetFileLoggingEnabled(value);
    }

    private void SyncColorFromHsl()
    {
        if (_isUpdating) return;
        _isUpdating = true;
        PrimaryColor = HslToColor(Hue, Saturation, Luminance);
        _isUpdating = false;
    }

    private void UpdateColorAndHsl(Color color, bool updateHsl)
    {
        _isUpdating = true;
        PrimaryColor = color;
        if (updateHsl || !_isUpdating) // Logic to ensure HSL is in sync
        {
            var (h, s, l) = ColorToHsl(color);
            Hue = h;
            Saturation = s;
            Luminance = l;
        }
        _isUpdating = false;
    }

    private void ApplyPreview()
    {
        var tempColors = new ColorSettings { PrimaryColor = PrimaryColor.ToString() };
        _themeService.ApplyTheme(tempColors);
    }

    // HSL Conversion Logic
    private Color HslToColor(double h, double s, double l)
    {
        double c = (1 - Math.Abs(2 * l - 1)) * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = l - c / 2;
        double r = 0, g = 0, b = 0;

        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }

        return Color.FromRgb((byte)Math.Clamp((r + m) * 255, 0, 255), 
                            (byte)Math.Clamp((g + m) * 255, 0, 255), 
                            (byte)Math.Clamp((b + m) * 255, 0, 255));
    }

    private (double h, double s, double l) ColorToHsl(Color color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double d = max - min;

        double l = (max + min) / 2;
        double s = d == 0 ? 0 : d / (1 - Math.Abs(2 * l - 1));
        double h = 0;

        if (d != 0)
        {
            if (max == r) h = (g - b) / d % 6;
            else if (max == g) h = (b - r) / d + 2;
            else h = (r - g) / d + 4;
            h *= 60;
            if (h < 0) h += 360;
        }

        return (h, s, l);
    }
}
