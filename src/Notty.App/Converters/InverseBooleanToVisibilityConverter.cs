using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Notty.App.Converters;

/// <summary>True → Collapsed, False → Visible. Used to show the welcome screen when there is no workspace.</summary>
public sealed class InverseBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is Visibility.Visible;
}
