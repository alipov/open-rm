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
        // TODO:  move to another class?
        public static Byte[] SerializeToXml(Message msg)
        {
            var mem = new MemoryStream();
            var writer = XmlWriter.Create(mem);
            
            //TODO:  how to change this code to generic?
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
            Message message;
            var mem = new MemoryStream(msg);
            var reader = XmlReader.Create(mem);

            using (var woxalizer = new WoxalizerUtil())
            {
                message = (Message)woxalizer.Load(reader);
            }
            return message;
        }
    }
}
