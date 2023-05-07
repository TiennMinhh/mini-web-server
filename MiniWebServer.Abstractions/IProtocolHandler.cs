﻿using MiniWebServer.Abstractions.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using static MiniWebServer.Abstractions.ProtocolHandlerStates;

namespace MiniWebServer.Abstractions
{
    public interface IProtocolHandler
    {
        int ProtocolVersion { get; }
        Task<BuildRequestStates> ReadRequest(TcpClient tcpClient, IHttpRequestBuilder httpWebRequestBuilder, ProtocolHandlerData data);
        Task SendResponse(TcpClient tcpClient, IHttpResponseBuilder responseObjectBuilder, ProtocolHandlerData protocolHandlerData);
    }
}