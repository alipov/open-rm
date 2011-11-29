
namespace OpenRm.Common.Entities.Network.Messages
{
    public class WakeOnLanResponse : ResponseBase
    {
        public bool Succeeded;

        public WakeOnLanResponse() {}
        public WakeOnLanResponse(bool success)
        {
            Succeeded = success;
        }
    }
}
