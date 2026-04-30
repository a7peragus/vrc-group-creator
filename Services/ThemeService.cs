using MaterialDesignThemes.Wpf;
using System.Windows.Media;
using VrcGroupCreator.Models;

namespace VrcGroupCreator.Services;

public class ThemeService
{
    private readonly PaletteHelper _paletteHelper = new();

    public void ApplyTheme(ColorSettings colors)
    {
        try
        {
            var primary = (Color)ColorConverter.ConvertFromString(colors.PrimaryColor);
            
            // Maintain a premium dark base as requested/implied by "make colors like black"
            var background = Color.FromRgb(10, 10, 10); // Very dark/black
            var surface = Color.FromRgb(22, 22, 22);    // Slightly lighter for cards
            var text = Colors.White;

            var resources = System.Windows.Application.Current.Resources;

            // 1. Update the base Material Design theme
            var theme = _paletteHelper.GetTheme();
            theme.Background = background;
            theme.SetPrimaryColor(primary);
            _paletteHelper.SetTheme(theme);

            // 2. Aggressively override brushes for maximum visual impact and consistency
            resources["MaterialDesignBackground"] = new SolidColorBrush(background);
            resources["MaterialDesignPaper"] = new SolidColorBrush(surface);
            resources["MaterialDesignCardBackground"] = new SolidColorBrush(surface);
            resources["MaterialDesignBody"] = new SolidColorBrush(text);
            resources["MaterialDesignBodyLight"] = new SolidColorBrush(text) { Opacity = 0.7 };
            
            resources["PrimaryHueMidBrush"] = new SolidColorBrush(primary);
            
            // Determine if text on primary should be black or white for readability
            double luminance = (0.299 * primary.R + 0.587 * primary.G + 0.114 * primary.B) / 255;
            var contrastText = luminance > 0.5 ? Colors.Black : Colors.White;
            resources["PrimaryHueMidForegroundBrush"] = new SolidColorBrush(contrastText);
            
            // Divider and subtle elements
            resources["MaterialDesignDivider"] = new SolidColorBrush(text) { Opacity = 0.12 };
            resources["MaterialDesignFlatButtonRipple"] = new SolidColorBrush(primary) { Opacity = 0.12 };
        }
        catch (Exception)
        {
        }
    }
}
