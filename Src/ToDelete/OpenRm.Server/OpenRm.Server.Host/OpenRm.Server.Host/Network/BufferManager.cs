﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace OpenRm.Server.Host
{
    /// This class creates a single large buffer which can be divided up and assigned to SocketAsyncEventArgs objects for use
    /// with each socket I/O operation.  This enables buffers to be easily reused and gaurds against fragmenting heap memory.
    /// 
    /// The operations exposed on the BufferManager class are not thread safe.
    class BufferManager
    {
        int _numBytes;                 // the total number of bytes controlled by the buffer pool
        byte[] _buffer;                // the underlying byte array maintained by the Buffer Manager
        Stack<int> _freeIndexPool;     
        int _currentIndex;
        int _bufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            _numBytes = totalBytes;
            _currentIndex = 0;
            _bufferSize = bufferSize;
            _freeIndexPool = new Stack<int>();
        }

        // Allocates buffer space used by the buffer pool
        public void InitBuffer()
        {
            // create one big large buffer and divide that out to each SocketAsyncEventArg object
            _buffer = new byte[_numBytes];
        }

        // Assigns a buffer from the buffer pool to the specified SocketAsyncEventArgs object
        // Returns true if the buffer was successfully set, else false
        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if (_freeIndexPool.Count > 0)
            {
                args.SetBuffer(_buffer, _freeIndexPool.Pop(), _bufferSize);
            }
            else
            {
                if ((_numBytes - _bufferSize) < _currentIndex)
                {
                    return false;
                }
                args.SetBuffer(_buffer, _currentIndex, _bufferSize);
                _currentIndex += _bufferSize;
            }
            return true;
        }

        // Removes the buffer from a SocketAsyncEventArg object.  
        // This frees the buffer back to the buffer pool
        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            _freeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
