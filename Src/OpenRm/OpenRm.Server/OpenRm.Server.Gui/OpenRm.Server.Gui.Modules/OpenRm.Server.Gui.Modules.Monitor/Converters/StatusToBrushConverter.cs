using System;
using System.Windows.Data;
using System.Windows.Media;
using OpenRm.Common.Entities.Enums;

namespace OpenRm.Server.Gui.Modules.Monitor.Converters
{
    [ValueConversion(typeof(int), typeof(Brush))]
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (targetType != typeof(Brush))
                throw new InvalidOperationException("The target must be a integer");

            SolidColorBrush brush;

            switch ((EAgentStatus)value)
            {
                case EAgentStatus.Online:
                    brush = Brushes.Green;
                    break;
                case EAgentStatus.Offline:
                    brush = Brushes.Red;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("value should be Online or Offline");
                    break;
            }

            return brush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
