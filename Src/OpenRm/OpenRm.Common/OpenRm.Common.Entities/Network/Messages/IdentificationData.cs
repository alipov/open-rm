namespace OpenRm.Common.Entities.Network.Messages
{
    public class IdentificationData : ResponseBase
    {
        public string deviceName;       // computer name for Windows OS, or IMEI for mobile devices
        public string sn;               // serial number (for Windows OS only)
    }
}