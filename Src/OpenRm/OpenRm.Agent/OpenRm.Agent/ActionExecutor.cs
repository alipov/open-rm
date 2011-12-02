//using System.Net;
//using OpenRm.Agent.Actions;
//using OpenRm.Common.Entities;
//using OpenRm.Common.Entities.Network.Messages;

//namespace OpenRm.Agent
//{
//    class ActionExecutor
//    {
//        public void PerformAction(AgentAsyncUserToken token, RequestBase request)
//        {
//            //perform action and prepare the response to send:
//            ResponseBase response;
//            if (request is IdentificationDataRequest)
//            {
//                response = IdRetriever.Run();
//            }
//            else if (request is InstalledProgramsRequest)
//            {
//                response = InstalledProgramsRetriever.Run();
//            }
//            else if (request is IpConfigRequest)
//            {
//                //process only opened socket's IP
//                response = IpConfigDataRetriever.Run( ((IPEndPoint)token.Socket.LocalEndPoint).Address.ToString() );
//            }
//            else if (request is LockSessionRequest)
//            {
//                response = LockSessionExecutor.Run();
//            }
//            else if (request is OsInfoRequest)
//            {
//                response = OsInfoRetriever.Run();
//            }
//            else if (request is PerfmonDataRequest)
//            {
//                response = PerfmonDataRetriever.Run( (PerfmonDataRequest)request );     //provide which disk to monitor
//            }
//            else if (request is PingRequest)
//            {
//                response = PingExecutor.Run((PingRequest)request);
//            }
//            else if (request is RunProcessRequest)
//            {
//                response = ProcessExecutor.Run((RunProcessRequest)request);
//            }
//            else if (request is WakeOnLanRequest)
//            {
//                response = WakeOnLanExecutor.Run((WakeOnLanRequest) request);
//            }
//            else if (request is ShutdownRequest)
//            {
//                response = ShutdownExecutor.Run((ShutdownRequest) request);
//            }
//            else if (request is RestartRequest)
//            {
//                response = RestartExecutor.Run((RestartRequest)request);
//            }
//            else
//            {
//                Logger.WriteStr("Error: got UNKNOWN request. Simply ignore it.");
//                return;
//            }

//            //check if action has returned any data
//            if (response != null)
//            {
//                //send the message
//                var msg = new ResponseMessage { Response = response };
//                //TODO: change call to SendMessage
//                //TcpClient.SendMessage(token.writeEventArgs, WoxalizerAdapter.SerializeToXml(msg));  
//            }

//        }
//    }
//}
