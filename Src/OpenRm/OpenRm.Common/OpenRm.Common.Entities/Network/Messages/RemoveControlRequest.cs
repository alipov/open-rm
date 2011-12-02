namespace OpenRm.Common.Entities.Network.Messages
{
    public class RemoteControlRequest : RequestBase
    {
        public string ViewerIp;
        public int ViewerPort;

        public  RemoteControlRequest() { }

        public RemoteControlRequest(string viewerIp, int viewerPort)
        {
            ViewerIp = viewerIp;
            ViewerPort = viewerPort;
        }
    }
}