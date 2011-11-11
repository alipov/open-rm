namespace OpenRm.Common.Entities.Network.Messages
{
    public class OsInfo : ResponseBase
    {
        public string OsName;
        public string OsVersion;
        public string OsArchitecture;
        public int CdriveSize;
        public int CdriveFreeSpace;
        public int RamSize;
        public int FreeRamSize;

    }
}