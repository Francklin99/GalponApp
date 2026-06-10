using System;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace GalponApp.Presentation.ViewModels
{
    /// <summary>
    /// Convierte una cadena a bool/Visibility: true si la cadena NO está vacía.
    /// Uso en XAML: IsVisible="{Binding MiPropiedad, Converter={x:Static vm:StringNotEmptyConverter.Instance}}"
    /// </summary>
    public class StringNotEmptyConverter : IValueConverter
    {
        public static readonly StringNotEmptyConverter Instance = new();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value as string);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
