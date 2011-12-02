
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
    }
}
