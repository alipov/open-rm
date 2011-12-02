﻿namespace OpenRm.Common.Entities.Network.Messages
{

    public abstract class Message
    {
        public int OpCode;         // int type (not Enum) because of Serialization    //TODO: delete it
        public int agentId;        //agent's identification (to match console-host-agent messages) //TODO: maybe change to Name...
    }

    public abstract class RequestBase
    {
    }

    public abstract class ResponseBase
    {
    }
}
