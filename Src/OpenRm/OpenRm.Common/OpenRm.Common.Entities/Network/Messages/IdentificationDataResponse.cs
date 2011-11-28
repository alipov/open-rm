namespace OpenRm.Common.Entities.Network.Messages
{
    public class IdentificationDataResponse : ResponseBase
    {
        // Computer name and motherboard serial number are client identifiers
        // IP/MAC address cannot be identifiers because it can be changed (i.e. by changing connection from LAN to WiFi)
        public string deviceName;       // computer name for Windows OS, or IMEI for mobile devices
        public string sn;               // serial number (for Windows OS only)
    }
}