using System;
using System.IO;
using System.Reflection;
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
            var writer = XmlWriter.Create(mem);
            
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

            return mem.ToArray();
        }

        public static Message DeserializeFromXml(Byte[] msg, Func<object, ResolveEventArgs, Assembly> assemblyResolveHandler)
        {
            Message message = null;
            var mem = new MemoryStream(msg);
            var reader = XmlReader.Create(mem);

            using (var woxalizer = new WoxalizerUtil())
            {
                try
                {
                    message = (Message)woxalizer.Load(reader);
                }
                catch(Exception)
                {
                    Logger.WriteStr("Cannot deserilize recieved object!");
                }
            }
            return message;
        }
    }
}
