﻿
using System.Net.Sockets;

namespace TomP2P.Message
{
    public interface IResponder
    {
        Message Response(Message responseMessage, bool isUdp, Socket channel); // last 2 params .NET-specific -> used in Dispatcher.Respond()

        Message Failed(Message.MessageType type, bool isUdp, Socket channel); // last 2 params .NET-specific -> used in Dispatcher.Respond()

        void ResponseFireAndForget(bool isUdp);
    }
}