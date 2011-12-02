using System.Collections.Generic;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Text;

namespace OpenRm.Common.Entities.Executors
{
    public static class PingExecutor
    {

        //public static ResponseBase Run( PingRequest request )
        //{
        //    //start with maximum ttl (32) in order to make just one ping
        //    var resultList = (List<string>)SendPingWithTtl(request.Target, 32);   
        //    var resultString = "";
        //    foreach (string str in resultList)
        //    {
        //        resultString += str + "\n";
        //    }
        //    return new RunCommonResponse(request.RunId, resultString);
        //}


        public static IEnumerable<string> SendPingWithTtl(string target, int ttl)
        {
            const int timeout = 5000;          // 5 sec timeout
            const int maxTtl = 32;             // Max TTL
            var result = new List<string>();

            Ping ping = new Ping();
            PingOptions options = new PingOptions(ttl, true);
            byte[] buffer = Encoding.ASCII.GetBytes("abcdefghigklmnop");    // send some data

            try
            {
                PingReply reply = ping.Send(target, timeout, buffer, options);

                if (reply != null)
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        result.Add(reply.Address.ToString() + "   " +
                            reply.RoundtripTime.ToString(NumberFormatInfo.CurrentInfo) + "ms");
                    }
                    else if (reply.Status == IPStatus.TtlExpired || reply.Status == IPStatus.TimedOut)
                    {
                        if (reply.Status == IPStatus.TtlExpired)
                        {
                            //add the currently returned address
                            result.Add(reply.Address.ToString());
                        }
                        else
                        {
                            result.Add(GetPingStatus(reply.Status));
                        }

                        if (ttl <= maxTtl)
                        {
                            //recurse to get the next address...
                            IEnumerable<string> tempResult = SendPingWithTtl(target, ttl + 1);
                            result.AddRange(tempResult);
                        }
                        else
                        {
                            result.Add("Has reached defined maximum TTL (" + maxTtl + ").");
                        }
                    }
                    else
                    {
                        result.Add(GetPingStatus(reply.Status));
                    }
                }
                else
                {
                    result.Add("Error occured while sending ping. Please see log for more info...");
                }

            }
            catch (PingException ex)
            {
                result.Add("Error occured while sending ping: " + ex.Message);
            }

            return result;
        }

        private static string GetPingStatus(IPStatus status)
        {
            switch (status)
            {
                case IPStatus.Success:
                    return "Success.";
                case IPStatus.DestinationHostUnreachable:
                    return "Destination host unreachable.";
                case IPStatus.DestinationNetworkUnreachable:
                    return "Destination network unreachable.";
                case IPStatus.DestinationPortUnreachable:
                    return "Destination port unreachable.";
                case IPStatus.DestinationProtocolUnreachable:
                    return "Destination protocol unreachable.";
                case IPStatus.PacketTooBig:
                    return "Packet too big.";
                case IPStatus.TtlExpired:
                    return "TTL expired.";
                case IPStatus.ParameterProblem:
                    return "Parameter problem.";
                case IPStatus.SourceQuench:
                    return "Source quench.";
                case IPStatus.TimedOut:
                    return "Request Timed out.";
                default:
                    return "Ping failed.";
            }
        }

    }
}
