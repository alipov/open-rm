using System.Net;

namespace OpenRm.Common.Entities.Network.Messages
{
    public class BulkStaticRequest: RequestBase
    {
        public IPAddress IpAddress { get; set; }

        public override ResponseBase ExecuteRequest()
        {
            var ipConfRequest = new IpConfigRequest()
                                    {
                                        IpAddress = IpAddress
                                    };
            var osInfoRequest = new OsInfoRequest();

            var response = new BulkStaticResponse()
                               {
                                   IpConf = (IpConfigResponse) ipConfRequest.ExecuteRequest(),
                                   OsInfo = (OsInfoResponse) osInfoRequest.ExecuteRequest()
                               };

            return response;
        }
    }
}
