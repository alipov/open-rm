using System.Net.Sockets;

namespace OpenRm.Server.Host.MicrosoftExample
{
    /// <summary>
    /// This class is designed for use as the object to be assigned to the SocketAsyncEventArgs.UserToken property. 
    /// </summary>
    class AsyncUserToken
    {
        public Socket Socket { get; set; }

        public AsyncUserToken() : this(null) { }

        public AsyncUserToken(Socket socket)
        {
            Socket = socket;
        }
    }
}
