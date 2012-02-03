using System;
using System.Net.Sockets;
using System.Threading;

namespace OpenRm.Common.Entities.Network
{
    public abstract class AsyncUserTokenBase
    {
        public Socket Socket { get; set; }

        // holds recieved message data (without prefix). used for storing partial messages also
        public byte[] RecievedMsgData { get; set; }
        public int RecievedMsgPartLength { get; set; }      // last enteres byte index

        // holds recieved prefix for cases when we get only a part of prefix, and need to call Receive method one more time
        public byte[] PrefixData { get; set; }
        public int RecievedPrefixPartLength { get; set; }   // last enteres byte index
        public int MessageLength { get; set; }              // holds recieved prefix as Integer

        // hold message to be sent
        public byte[] SendingMsg { get; set; }
        public int SendingMsgBytesSent { get; set; }    //last sent byte sequence number

        //for recieving Args: hold sendig Args in token
        public SocketAsyncEventArgs writeEventArgs { get; set; }
        public SocketAsyncEventArgs readEventArgs { get; set; }

        public Semaphore writeSemaphore;
        public Semaphore readSemaphore;

        public System.Timers.Timer KeepAliveTimer { get; set; }

        //protected AsyncUserTokenBase() : this(null, null) { }

        protected readonly int _msgPrefixLength;

        protected AsyncUserTokenBase(Socket socket, int msgPrefixLength = 4)
        {
            _msgPrefixLength = msgPrefixLength;
            Socket = socket;
            PrefixData = new Byte[_msgPrefixLength];
            RecievedPrefixPartLength = 0;
            RecievedMsgPartLength = 0;
            writeEventArgs = null;
            readEventArgs = null;
            readSemaphore = new Semaphore(1, 1);
            writeSemaphore = new Semaphore(1, 1);
        }


        // Prepare token for reuse
        public void CleanForSend()
        {
            SendingMsg = null;
            SendingMsgBytesSent = 0;
        }

        public void CleanForRecieve()
        {
            RecievedMsgData = null;
            PrefixData = new Byte[_msgPrefixLength];
            RecievedMsgPartLength = 0;
            RecievedPrefixPartLength = 0;
            MessageLength = 0;
        }

        public void Wipe()
        {
            CleanForSend();
            CleanForRecieve();
            writeEventArgs = null;
            readEventArgs = null;
        }
    }
}
