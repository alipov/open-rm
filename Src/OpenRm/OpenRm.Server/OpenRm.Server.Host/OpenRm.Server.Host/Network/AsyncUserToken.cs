﻿//using System;
//using System.Net.Sockets;
//using OpenRm.Common.Entities;

//namespace OpenRm.Server.Host
//{
//    //class AsyncUserToken
//    //{
//    //    public Socket socket { get; set; }
//    //    public ClientData data { get; set; }

//    //    // holds recieved message data (without prefix). used for storing partial messages also
//    //    public byte[] msgData { get; set; }
//    //    public int recievedMsgPartLength { get; set; }      // last enteres byte index

//    //    // holds recieved prefix for cases when we get only a part of prefix, and need to call Receive method one more time
//    //    public byte[] prefixData { get; set; }
//    //    public int recievedPrefixPartLength { get; set; }   // last enteres byte index
//    //    public int messageLength { get; set; }              // holds recieved prefix as Integer

//    //    // hold message to be sent
//    //    public byte[] sendingMsg { get; set; }
//    //    public int sendingMsgBytesSent { get; set; }    //last sent byte sequence number

//    //    public static int runId = 1;                    //execution identification number: to mark each remote execution with unique id

//    //    public AsyncUserToken() : this(null, null) { }

//    //    public AsyncUserToken(Socket socket, ClientData data)
//    //    {
//    //        this.socket = socket;
//    //        this.data = data;
//    //        prefixData = new Byte[TCPServerListener.msgPrefixLength];       // 4 bytes prefix
//    //        this.recievedPrefixPartLength = 0;
//    //        this.recievedMsgPartLength = 0;
//    //    }

//    //    // Prepare token for reuse
//    //    public void Clean()
//    //    {
//    //        msgData = null;
//    //        sendingMsg = null;
//    //        prefixData = new Byte[TCPServerListener.msgPrefixLength];
//    //        recievedMsgPartLength = 0;
//    //        recievedPrefixPartLength = 0;
//    //        messageLength = 0;
//    //        sendingMsgBytesSent = 0;
//    //    }

//    //    // returns and increments process identification number
//    //    public int GetRunId()
//    //    {
//    //        return runId++;
//    //    }
//    //}
//}
