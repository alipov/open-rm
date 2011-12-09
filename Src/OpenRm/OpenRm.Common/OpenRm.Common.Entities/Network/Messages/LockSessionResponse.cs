namespace OpenRm.Common.Entities.Network.Messages
{
    public class LockSessionResponse : ResponseBase
    {
        public bool Succeeded;

        public override string ToString()
        {
            string message;

            if (Succeeded)
            {
                message = "Session lock succeeded.";
            }
            else
            {
                message = "Session lock failed.";
            }

            return message;
        }
    }
}
