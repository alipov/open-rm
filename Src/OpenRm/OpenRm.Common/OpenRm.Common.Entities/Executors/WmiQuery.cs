using System;
using System.Collections.Generic;
using System.Management;

namespace OpenRm.Common.Entities.Executors
{
    public static class WmiQuery
    {

        public static string GetWMIdata(string key, string property)
        {
            return GetWMIdata(key, property, null, null);
        }


        public static string GetWMIdata(string key, string property, string specificElementName, string specificElementValue)
        {
            string[] properties = new string[] { property };     // needed only for providing to another function

            Dictionary<string, string> values = GetWMIdata(key, properties, specificElementName, specificElementValue);

            return values[property];
        }


        public static Dictionary<string, string> GetWMIdata(string key, string[] properties)
        {
            return GetWMIdata(key, properties, null, null);
        }


        public static Dictionary<string, string> GetWMIdata(string key, string[] properties, string specificElementName, string specificElementValue)
        {
            var values = new Dictionary<string, string>();      //return value
            try
            {
                var searcher = new ManagementObjectSearcher("select * from " + key);
                foreach (ManagementObject element in searcher.Get())
                {
                    if (specificElementName == null || specificElementValue.Equals(element[specificElementName].ToString()) )
                    {
                        foreach (string property in properties)
                        {
                            try
                            {
                                values.Add(property, element[property].ToString());
                            }
                            catch (Exception)
                            {
                                // just ignore exception bacause it some properties don't exist in all OS platforms (like OSArchitecture)
                                Logger.WriteStr(" WARNING: \"" + property + "\" does not exist in " + key);
                            }
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.WriteStr(" ERROR: Cannot retrieve data from WMI key " + key + ". (Error: " + ex.Message + ")");
                throw new ArgumentException("Cannot retrieve required WMI data.");
            }

            return values;
        }

    }
}
