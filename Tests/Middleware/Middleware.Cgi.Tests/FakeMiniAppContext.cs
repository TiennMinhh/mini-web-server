﻿using MiniWebServer.Abstractions;
using MiniWebServer.MiniApp;
using MiniWebServer.WebSocket.Abstractions;
using System.Security.Claims;

namespace MvcMiddlewareTests
{
    internal class FakeMiniAppContext(Func<IHttpRequest> request, Func<IHttpResponse> response) : IMiniAppRequestContext
    {
        private readonly IHttpRequest request = request();
        private readonly IHttpResponse response = response();

        public IHttpRequest Request => request;

        public IHttpResponse Response => response;

        public ISession Session { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IServiceProvider Services => throw new NotImplementedException();

        public ClaimsPrincipal? User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        IWebSocketManager IMiniAppContext.WebSockets { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
