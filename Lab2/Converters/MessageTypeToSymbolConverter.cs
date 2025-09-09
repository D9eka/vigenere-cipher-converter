using Lab2.Services.Message;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace Lab2.Converters
{
    public class MessageTypeToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MessageType type)
            {
                return type switch
                {
                    MessageType.Error => Symbol.Cancel,       // крестик
                    MessageType.Warning => Symbol.Repair,    // предупреждение
                    _ => Symbol.Help                        // нейтральная иконка
                };
            }
            return Symbol.Help;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotSupportedException();
    }
}
