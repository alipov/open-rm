using System.Windows.Data;

namespace OpenRm.Server.Gui.Modules.Monitor.Converters
{
    public static class StaticConverters
    {
        public static IValueConverter InstanceToBooleanConverter = new InstanceToBooleanConverter();
        public static IValueConverter InvertBoolConverter = new InvertBoolConverter();
    }
}
