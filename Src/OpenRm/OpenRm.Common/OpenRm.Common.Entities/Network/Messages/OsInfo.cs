namespace OpenRm.Common.Entities.Network.Messages
{
    public class OsInfo : ResponseBase
    {
        public string OsName;
        public string OsVersion;
        public string OsArchitecture;
        public int RamSize;
        //public int FreeRamSize;
        public string SystemDrive;
        public int SystemDriveSize;
        //public int SystemDriveFree;
    }
}