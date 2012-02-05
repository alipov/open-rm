using System;
using System.Globalization;
using System.Windows.Data;
using OpenRm.Common.Entities.Enums;
using OpenRm.Server.Gui.Modules.Monitor.Models;

namespace OpenRm.Server.Gui.Modules.Monitor.Converters
{
    public class InstanceToBooleanInverseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                if (((AgentWrapper)value).Status == (int)EAgentStatus.Offline)
                    return true;
            }
            return false;
        }

        //public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    return value != null;
        //}

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Should not use ConvertBack");
        }
    }
}
