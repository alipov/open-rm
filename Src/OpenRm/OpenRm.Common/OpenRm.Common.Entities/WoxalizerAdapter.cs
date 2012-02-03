using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using OpenRm.Common.Entities.Network.Messages;
using Woxalizer;

namespace OpenRm.Common.Entities
{
    public static class WoxalizerAdapter
    {
        
        public static Byte[] SerializeToXml(Message msg)
        {
            var mem = new MemoryStream();

            //Set XML settings to not add "Byte Order Mark" (three bytes appended at beginning of byte array)
            var xmlSettings = new XmlWriterSettings {Encoding = new UTF8Encoding(false)};       

            using (XmlWriter writer = XmlWriter.Create(mem, xmlSettings))
            {
                using (var woxalizer = new WoxalizerUtil())
                {
                    if (msg is RequestMessage)
                    {
                        woxalizer.Save((RequestMessage)msg, writer);
                    }
                    else if (msg is ResponseMessage)
                    {
                        woxalizer.Save((ResponseMessage)msg, writer);
                    }
                    else
                    {
                        Logger.WriteStr("ERROR in serialization method: cannot determinate message type.");
                    }
                }
            }

            return mem.ToArray();
        }

        public static Message DeserializeFromXml(Byte[] msg)
        {
            Message message = null;
            var mem = new MemoryStream(msg);

            var reader = XmlReader.Create(mem);

            using (var woxalizer = new WoxalizerUtil())
            {
                // can throw exception
                message = (Message)woxalizer.Load(reader);
            }
            return message;
        }


        private static readonly object lck = new object();     // for handeling Writes from many threads

        public static void SaveToFile(object ob, string filename)
        {
            lock (lck)
            {
                using (var woxalizer = new WoxalizerUtil())
                {
                    try
                    {
                        woxalizer.Save(ob, filename);
                    }
                    catch (Exception)
                    {
                        Logger.WriteStr("ERROR: Unable to serialize object to " + filename + "!");
                    }
                }
            }
        }

        public static object LoadFromFile(string filename)
        {
            object ob = null;
            using (var woxalizer = new WoxalizerUtil())
            {
                try
                {
                    ob = woxalizer.Load(filename);
                }
                catch (Exception)
                {
                    Logger.WriteStr("DEBUG: Unable to load object from " + filename + "!");
                }
            }
            return ob;
        }


    }
}
