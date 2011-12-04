namespace OpenRm.Common.Entities.Network.Messages
{
    public class OsInfoResponse : ResponseBase
    {
        public string OsName;
        public string OsVersion;
        public string OsArchitecture;
        public int RamSize;
        public string SystemDrive;
        public int SystemDriveSize;     //in Gb
    }
}