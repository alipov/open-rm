namespace OpenRm.Common.Entities.Network.Messages
{
    public class OsInfoResponse : ResponseBase
    {
        public string OsName { get; set; }
        public string OsVersion { get; set; }
        public string OsArchitecture { get; set; }
        public int RamSize { get; set; }
        public string SystemDrive { get; set; }
        public int SystemDriveSize { get; set; }    //in Gb
    }
}