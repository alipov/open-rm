namespace OpenRm.Common.Entities
{
    public class Message
    {
        public int Id;
        public CommandBase Command;

        //public string 
    }

    public abstract class CommandBase
    {
        public string Info;
    }

    public class IdentificationData : CommandBase
    {
        public string deviceName;       // computer name for Windows OS, or IMEI for mobile devices
        public string sn;               // serial number (for Windows OS only)
    }

    public class IpConfigData : CommandBase
    {
        public string IpAddress;
        public string netMask;
        public string defaultGateway;
    }
}
