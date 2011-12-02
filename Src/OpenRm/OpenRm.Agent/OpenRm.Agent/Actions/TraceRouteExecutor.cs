//using System.Collections.Generic;
//using OpenRm.Common.Entities.Executors;
//using OpenRm.Common.Entities.Network.Messages;

//namespace OpenRm.Agent.Actions
//{
//    public static class TraceRouteExecutor
//    {
//        public static ResponseBase Run(int runId, string target)
//        {
//            var resultList = (List<string>)PingExecutor.SendPingWithTtl(target, 1);     //start with minimum ttl in order to increase it recursively
//            var resultString = "";
//            foreach (string str in resultList)
//            {
//                resultString += str + "\n";
//            }

//            return new RunCommonResponse(runId, resultString);
//        }

//        /* To get data: 
//                var l = (List<string>)TraceRoute("8.8.8.8");
//                foreach (string s in l)
//                {
//                    Console.WriteLine(s);    
//                } 
//         */
//    }
//}
