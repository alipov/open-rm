﻿using System;
using System.Net.Sockets;

namespace OpenRm.Common.Entities.Network
{
    public class GeneralUserToken : AsyncUserTokenBase
    {
        public Action<CustomEventArgs> Callback { get; set; }

        public GeneralUserToken(Socket socket, int msgPrefixLength = 4)
            : this(socket, null, msgPrefixLength){ }

        public GeneralUserToken(Socket socket, Action<CustomEventArgs> callback, int msgPrefixLength = 4)
            : base(socket, msgPrefixLength)
        {
            Callback = callback;
        }
    }
}
