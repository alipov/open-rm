using System.Net.Sockets;

namespace OpenRm.Server.Host.MicrosoftExample
{
    /// <summary>
    /// This class is designed for use as the object to be assigned to the SocketAsyncEventArgs.UserToken property. 
    /// </summary>
    class AsyncUserTokenMicr
    {
        public Socket Socket { get; set; }

        public AsyncUserTokenMicr() : this(null) { }

        public AsyncUserTokenMicr(Socket socket)
        {
            Socket = socket;
        }
    }
}
