using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Lab2.Services.Message;

namespace Lab2.Converters;

public class MessageTypeToBrushBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is MessageType type)
        {
            switch (type)
            {
                case MessageType.Warning:
                    return (SolidColorBrush)Application.Current.Resources["WarningBackgroundBrush"];
                case MessageType.Error:
                    return (SolidColorBrush)Application.Current.Resources["ErrorBackgroundBrush"];
                default:
                    return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }
        }

        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
