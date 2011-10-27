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

    public class IntoData : CommandBase
    {
        public string computerName;
        public string id;
    }

    public class IpConfigData : CommandBase
    {
        public string IpAddress;
        public string netMask;
        public string defaultGateway;
    }
}
