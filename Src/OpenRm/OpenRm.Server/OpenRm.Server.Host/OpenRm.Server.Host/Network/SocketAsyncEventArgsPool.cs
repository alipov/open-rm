using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace OpenRm.Server.Host.Network
{
    // Represents a collection of resusable SocketAsyncEventArgs objects.  
    class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> m_pool;
        
        // Initializes the object pool to the specified size
        public SocketAsyncEventArgsPool(int capacity)
        {
            m_pool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        /// Add a SocketAsyncEventArg instance to the pool
        public void Push(SocketAsyncEventArgs item)
        {
            if (item == null) { throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null"); }
            lock (m_pool)
            {
                m_pool.Push(item);
            }
        }

        // Removes a SocketAsyncEventArgs instance from the pool
        public SocketAsyncEventArgs Pop()
        {
            lock (m_pool)
            {
                return m_pool.Pop();
            }
        }

        // The number of SocketAsyncEventArgs instances in the pool
        public int Count
        {
            get { return m_pool.Count; }
        }


    }
}
