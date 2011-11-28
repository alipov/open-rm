using OpenRm.Common.Entities;
using OpenRm.Common.Entities.Network;
using OpenRm.Common.Entities.Network.Messages;
using OpenRm.Common.Entities.Network.Server;

namespace OpenRm.Agent.Actions
{
    class ActionExecutor
    {
        public static void PerformAction(TcpServerListenerAdv server, HostAsyncUserToken token, RequestBase request)
        {
            //perform action and prepare the response to send:
            ResponseBase response;
            if (request is IdentificationDataRequest)
            {
                response = IdRetriever.Run();
            }
            else if (request is InstalledProgramsRequest)
            {
                response = InstalledProgramsRetriever.Run();
            }
                //................



            else
            {
                Logger.WriteStr("Error: got UNKNOWN request. Simply ignore it.");
                return;
            }

            //send the message
            var msg = new ResponseMessage { Response = response };
            server.Send(msg, token);
            
            

        }
    }
}
