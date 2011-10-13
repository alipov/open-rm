using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using OpenRm.Server.Host.Network;
using System.Net.Sockets;


namespace OpenRm.Server.Host
{
    class Server
    {
        public static int port = 3777;
        public static int maxNumConnections = 1000;     //maximum number of connections
        public static int receiveBufferSize = 512;            //recieve buffer size for connection
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        Socket listenSocket;            // the socket used to listen for incoming connection requests
        // pool of reusable SocketAsyncEventArgs objects for write, read and accept socket operations
        SocketAsyncEventArgsPool m_readWritePool;
        
        static void Main(string[] args)
        {

            
            

            TCPServerListener srv = new TCPServerListener(port, maxNumConnections, receiveBufferSize, m_bufferManager, m_readWritePool, localEndPoint);
            



        }


    }
}
