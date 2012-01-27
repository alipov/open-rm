
namespace OpenRm.Common.Entities.Network.Messages
{
    public class WakeOnLanResponse : ResponseBase
    {
        public bool Succeeded;
        public int RunId;

        public WakeOnLanResponse() {}
        public WakeOnLanResponse(bool success, int runId)
        {
            Succeeded = success;
            RunId = runId;
        }

        public override string ToString()
        {
            if (Succeeded)
                return "Wake on LAN succeeded.";
            return "Wake on LAN failed.";
        }
    }
}
