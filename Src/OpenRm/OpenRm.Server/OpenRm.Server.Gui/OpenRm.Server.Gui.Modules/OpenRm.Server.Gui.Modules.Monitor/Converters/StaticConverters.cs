using System.Windows.Data;

namespace OpenRm.Server.Gui.Modules.Monitor.Converters
{
    public static class StaticConverters
    {
        public static IValueConverter InstanceToBooleanConverter = new InstanceToBooleanConverter();
        public static IValueConverter InstanceToBooleanInverseConverter = new InstanceToBooleanInverseConverter();
        public static IValueConverter InvertBoolConverter = new InvertBoolConverter();
        public static IValueConverter DummyValueConverter = new DummyValueConverter();
        public static IValueConverter StatusToBrushConverter = new StatusToBrushConverter();
    }
}
