namespace OpenRm.Common.Entities.Network.Messages
{
    public class BulkStaticResponse : ResponseBase
    {
        // Contrains bulk of static info classes, to group some Console requests 
        public IpConfigResponse IpConf;
        public OsInfoResponse OsInfo;
        // (can containg more objects... - as needed)
    }


}
