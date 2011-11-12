using System;
using System.Net;
using System.Net.Sockets;
using OpenRm.Common.Entities.Network.Messages;

namespace OpenRm.Common.Entities.Network
{
    internal class SockAdvClient
    {
        private readonly Socket _clientSocket;
        private readonly IPEndPoint _hostEndPoint;
        private bool _isConnected;

        internal SockAdvClient(String hostName, Int32 port)
        {
            // Get host related information.
            IPHostEntry host = Dns.GetHostEntry(hostName);

            // Addres of the host.
            IPAddress[] addressList = host.AddressList;

            // Instantiates the endpoint and socket.
            _hostEndPoint = new IPEndPoint(addressList[addressList.Length - 1], port);
            _clientSocket = new Socket(_hostEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        }

        internal void Connect(Action<CustomEventArgs> callback)
        {
            var connectArgs = new SocketAsyncEventArgs
                                  {
                                      UserToken = callback, 
                                      RemoteEndPoint = _hostEndPoint
                                  };

            connectArgs.Completed += OnConnectCompleted;
            _clientSocket.ConnectAsync(connectArgs);
        }

        internal void Send(Message message, Action<CustomEventArgs> callback)
        {
            if (!_isConnected)
                throw new SocketException((Int32)SocketError.NotConnected);

            // Create a buffer to send.
            Byte[] sendBuffer = WoxalizerAdapter.SerializeToXml(message, null);

            // Prepare arguments for send/receive operation.
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(sendBuffer, 0, sendBuffer.Length);
            socketArgs.UserToken = callback;
            socketArgs.RemoteEndPoint = _hostEndPoint;
            socketArgs.Completed += OnSendCompleted;
            
            // Start sending asyncronally.
            _clientSocket.SendAsync(socketArgs);
        }

        internal void Disconnect(Action<CustomEventArgs> callback)
        {
            // Prepare arguments for send/receive operation.
            var socketArgs = new SocketAsyncEventArgs();
            socketArgs.SetBuffer(new byte[0], 0, 0);
            socketArgs.UserToken = callback;
            socketArgs.RemoteEndPoint = _hostEndPoint;
            socketArgs.Completed += OnDisconnectCompleted;

            _clientSocket.DisconnectAsync(socketArgs);
        }

        private void OnDisconnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                throw new SocketException((int)e.SocketError);
            }

            _isConnected = false;

            var callback = e.UserToken as Action<CustomEventArgs>;

            if (callback != null)
                callback.Invoke(new CustomEventArgs(e.SocketError, null));
        }

        private void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError != SocketError.Success)
            {
                throw new SocketException((int)e.SocketError);
            }

            _isConnected = true;

            var callback = e.UserToken as Action<CustomEventArgs>;

            if(callback != null)
                callback.Invoke(new CustomEventArgs(e.SocketError, null));
        }

        private void OnSendCompleted(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of send.
            //autoSendReceiveEvents[ReceiveOperation].Set();

            if (e.SocketError == SocketError.Success)
            {
                if (e.LastOperation == SocketAsyncOperation.Send)
                {
                    byte[] receiveBuffer = new byte[255];
                    e.SetBuffer(receiveBuffer, 0, receiveBuffer.Length);
                    e.Completed += OnReceiveCompleted;
                    _clientSocket.ReceiveAsync(e);
                }
            }
            else
            {
                ProcessError(e);
            }
        }

        private void OnReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            // Signals the end of receive.
            //autoSendReceiveEvents[SendOperation].Set();
            if (e.SocketError == SocketError.Success)
            {
                var callback = e.UserToken as Action<CustomEventArgs>;
                if (callback != null)
                {
                    var bytes = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, e.Offset, bytes, 0, e.BytesTransferred);
                    Message message = WoxalizerAdapter.DeserializeFromXml(e.Buffer, null);

                    callback.Invoke(new CustomEventArgs(e.SocketError, message));

                }
            }
        }

        private void ProcessError(SocketAsyncEventArgs e)
        {
            Socket s = e.UserToken as Socket;
            if (s.Connected)
            {
                // close the socket associated with the client
                try
                {
                    s.Shutdown(SocketShutdown.Both);
                }
                catch (Exception)
                {
                    // throws if client process has already closed
                }
                finally
                {
                    if (s.Connected)
                    {
                        s.Close();
                    }
                }
            }

            // Throw the SocketException
            throw new SocketException((Int32)e.SocketError);
        }
    }
}
