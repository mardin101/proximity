using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Proximity.UI.Converters;

/// <summary>
/// Converts a boolean to Visibility (true = Visible, false = Collapsed)
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            // If parameter is "Invert", reverse the logic
            if (parameter is string s && s == "Invert")
                return b ? Visibility.Collapsed : Visibility.Visible;

            return b ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

/// <summary>
/// Converts an enum to Visibility based on parameter match
/// </summary>
public class EnumToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;
        return value.ToString() == parameter.ToString() ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts a boolean mute state to a display string
/// </summary>
public class MuteStateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isMuted && isMuted ? "🔇 Unmute" : "🎤 Mute";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
