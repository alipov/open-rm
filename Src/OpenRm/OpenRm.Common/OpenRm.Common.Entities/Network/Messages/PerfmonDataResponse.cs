namespace OpenRm.Common.Entities.Network.Messages
{
    public class PerfmonDataResponse : ResponseBase
    {
        public int CPUuse;              // in %
        public int RAMfree;             // in Mb
        public int DiskFree;            // in Mb
        public float DiskQueue;         // Averege disk queue length
    }
}
