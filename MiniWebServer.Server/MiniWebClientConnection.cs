﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniWebServer.Abstractions;
using MiniWebServer.Abstractions.Http;
using MiniWebServer.MiniApp;
using MiniWebServer.Server.Abstractions;
using MiniWebServer.Server.Http;
using MiniWebServer.Server.Http.Helpers;
using MiniWebServer.Server.MiniApp;
using MiniWebServer.Server.Session;
using MiniWebServer.WebSocket.Abstractions;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Net;
using System.Reflection.PortableExecutable;
using System.Text;

namespace MiniWebServer.Server
{
    public class MiniWebClientConnection
    {
        private static readonly byte[] HTTP2_MAGIC = Encoding.ASCII.GetBytes("PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n");


        public MiniWebClientConnection(
            MiniWebConnectionConfiguration config,
            IProtocolHandlerFactory protocolHandlerFactory,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken
            )
        {
            ConnectionId = config.Id;

            this.config = config ?? throw new ArgumentNullException( nameof( config ) );
            this.protocolHandlerFactory = protocolHandlerFactory ?? throw new ArgumentNullException( nameof( protocolHandlerFactory ) );
            this.cancellationToken = cancellationToken;
            this.serviceProvider = serviceProvider;

            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            logger = loggerFactory.CreateLogger<MiniWebClientConnection>();
        }

        public ulong ConnectionId { get; }

        private readonly MiniWebConnectionConfiguration config;
        private readonly IProtocolHandlerFactory protocolHandlerFactory;
        private readonly ILogger logger;
        private readonly CancellationToken cancellationToken;
        private readonly IServiceProvider serviceProvider;

        public async Task HandleRequestAsync()
        {
            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.cancellationToken); // we will use this to keep control on connection timeout
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            bool isKeepAlive = true;

            try
            {
                if (config.TcpClient.Client.RemoteEndPoint == null) // this will never happen
                {
                    logger.LogError("TcpClient.Client.RemoteEndPoint == null");
                    isKeepAlive = false;
                }

                PipeReader requestPipeReader = PipeReader.Create(config.ClientStream);
                PipeWriter responsePipeWriter = PipeWriter.Create(config.ClientStream);

                ReadResult readResult = await requestPipeReader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                HttpVersions httpVersion = TryGetHttpVersion(buffer);
                if (httpVersion != HttpVersions.Http11 && httpVersion != HttpVersions.Http20)
                {
                    // unknown version
                    logger.LogError("Not supported HTTP version");
                    return;
                }

                if (httpVersion == HttpVersions.Http20) // skip MAGIC string
                {
                    requestPipeReader.AdvanceTo(buffer.GetPosition(HTTP2_MAGIC.Length));
                }

                var protocolConfig = new ProtocolHandlerConfiguration(httpVersion, config.MaxRequestBodySize);
                var protocolHandler = protocolHandlerFactory.Create(httpVersion, protocolConfig);

                MiniAppConnectionContext connectionContext = BuildMiniAppConnectionContext();

                while (isKeepAlive)
                {
                    cancellationTokenSource.CancelAfter(config.ReadRequestTimeout);

                    logger.LogDebug("[{cid}] - Reading request...", ConnectionId);

                    var requestBuilder = new HttpWebRequestBuilder();
                    // if time out we can simply close the connection
                    try
                    {
                        var requestId = config.RequestIdManager.GetNext();
                        var localEndPoint = config.TcpClient.Client.LocalEndPoint as IPEndPoint ?? throw new InvalidOperationException("TcpClient.Client.LocalEndPoint cannot be casted to IPEndPoint");
                        var remoteEndPoint = config.TcpClient.Client.RemoteEndPoint as IPEndPoint ?? throw new InvalidOperationException("TcpClient.Client.RemoteEndPoint cannot be casted to IPEndPoint");

                        requestBuilder
                            .SetRequestId(requestId)
                            .SetHttps(config.IsHttps)
                            .SetPort(localEndPoint.Port)
                            .SetRemoteAddress(remoteEndPoint.Address)
                            .SetRemotePort(remoteEndPoint.Port);

                        var readRequestResult = await protocolHandler.ReadRequestAsync(requestPipeReader, requestBuilder, cancellationToken);
                        if (!readRequestResult)
                        {
                            isKeepAlive = false; // we always close wrongly working connections

                            var response = new HttpResponse(HttpResponseCodes.BadRequest, config.ClientStream);

                            cancellationTokenSource.CancelAfter(config.SendResponseTimeout);
                            logger.LogDebug("[{cid}] - Sending back response...", ConnectionId); // send back Bad Request
                            await SendResponseAsync(response, protocolHandler, cancellationToken);

                            break;
                        }
                        else
                        {
                            isKeepAlive = false; // we will close the connection if there is any error while building request

                            var request = requestBuilder.Build();

                            isKeepAlive = request.KeepAliveRequested; // todo: we should have a look at how we manage a keep-alive connection later

                            var app = FindApp(request); // should we reuse apps???

                            var response = new HttpResponse(HttpResponseCodes.NotFound, config.ClientStream);

                            if (app != null)
                            {
                                cancellationTokenSource.CancelAfter(config.ExecuteTimeout);
                                logger.LogDebug("[{cid}][{rid}] - Processing request...", ConnectionId, requestId);

                                // now we continue reading body part
                                CancellationTokenSource readBodyCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                                Task readBodyTask = protocolHandler.ReadBodyAsync(requestPipeReader, request, readBodyCancellationTokenSource.Token);
                                Task callMethodTask = CallByMethod(connectionContext, app, request, response, cancellationToken);

                                readBodyCancellationTokenSource.Cancel();

                                // todo: here we need to find a proper way to stop reading body after calling to middlewares and endpoints finished
                                Task.WaitAll([readBodyTask, callMethodTask], cancellationToken);
                                logger.LogDebug("[{cid}][{rid}] - Done processing request...", ConnectionId, requestId);
                            }

                            if (connectionContext.WebSockets.IsUpgradeRequest)
                            {
                                // if this is a websocket request, then we always close the connection when it's done
                                // we don't send the response because in WebSocket middleware we have sent back an 'Upgrade' response, and 
                                // the connection is now a websocket connection (even if we have done nothing in websocket handler)
                                isKeepAlive = false;
                            }
                            else
                            {
                                var connectionHeader = response.Headers.Connection;
                                if (!"keep-alive".Equals(connectionHeader) && !"close".Equals(connectionHeader))
                                {
                                    response.Headers.Connection = isKeepAlive ? "keep-alive" : "close";
                                }

                                cancellationTokenSource.CancelAfter(config.SendResponseTimeout);
                                logger.LogDebug("[{cid}][{rid}] - Sending back response...", ConnectionId, requestId);
                                await SendResponseAsync(response, protocolHandler, cancellationToken);
                            }

                            //if (connectionContext.WebSockets.IsUpgradeRequest && app != null) // if this is an upgrade request, we switch to websocket mode and continue processing
                            //{
                            //    isKeepAlive = false; // we will always close the connection when this websocket operation ends

                            //    var webSocketFactory = connectionContext.Services.GetService<IWebSocketFactory>();
                            //    if (webSocketFactory == null)
                            //    {
                            //        logger.LogWarning("[{cid}][{rid}] - No websocket factory registered, closing connection", ConnectionId, requestId);
                            //    }
                            //    else if (connectionContext.WebSockets.Handler != null)
                            //    {
                            //        logger.LogInformation("[{cid}][{rid}] - Connection upgraded, transfering control to websocket handler", ConnectionId, requestId);
                            //        try
                            //        {
                            //            var webSocket = webSocketFactory.CreateWebSocket(config.ClientStream, config.ClientStream);

                            //            await connectionContext.WebSockets.Handler(webSocket);

                            //            await webSocket.CloseAsync(cancellationToken); // CloseAsync should silently skip if it is already in Close state
                            //        } catch (Exception ex)
                            //        {
                            //            logger.LogError(ex, "[{cid}][{rid}] - Error calling websocket handler", ConnectionId, requestId);
                            //        }
                            //    }
                            //    else
                            //    {
                            //        logger.LogWarning("[{cid}][{rid}] - No websocket handler defined, closing connection", ConnectionId, requestId);
                            //    }
                            //}
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        isKeepAlive = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{cid}] - Error processing request", ConnectionId);
            }
            finally
            {
                CloseConnection();
            }
        }

        private MiniAppConnectionContext BuildMiniAppConnectionContext()
        {
            //var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

            var connectionContext = new MiniAppConnectionContext(serviceProvider.CreateScope().ServiceProvider);

            return connectionContext;
        }

        private async Task SendResponseAsync(HttpResponse response, IProtocolHandler protocolHandler, CancellationToken cancellationToken)
        {
            await protocolHandler.WriteResponseAsync(response, cancellationToken);

            await response.Stream.FlushAsync(cancellationToken);
        }

        private IMiniApp? FindApp(HttpRequest request)
        {
            var host = request.Headers.Host;
            if (host == null)
            {
                return null;
            }

            if (config.HostContainers.TryGetValue(host.Host, out var container))
            {
                return container.App;
            }
            else
            {
                if (config.HostContainers.TryGetValue(string.Empty, out container)) // Host "" is a catch-all host
                {
                    return container.App;
                }
            }

            return null;
        }

        private void CloseConnection()
        {
            try
            {
                logger.LogDebug("[{cid}] - Closing connection...", ConnectionId);
                if (config.TcpClient.Connected)
                {
                    config.TcpClient.GetStream().Flush();
                }
                config.TcpClient.Close();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to close connection");
            }
        }

        private async Task CallByMethod(MiniAppConnectionContext connectionContext, IMiniApp app, HttpRequest request, IHttpResponse response, CancellationToken cancellationToken)
        {
            try
            {
                var context = BuildMiniContext(connectionContext, app, request, response);

                var action = app.Find(context);

                if (action != null)
                {
                    try
                    {
                        await action.InvokeAsync(context, cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "[{cid}] - Error executing action handler", ConnectionId);
                        response.StatusCode = HttpResponseCodes.InternalServerError;
                    }
                }
                else
                {
                    StandardResponseBuilderHelpers.NotFound(response);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[{cid}] - Error executing resource", ConnectionId);
            }
        }

        private static MiniAppContext BuildMiniContext(MiniAppConnectionContext connectionContext, IMiniApp app, IHttpRequest request, IHttpResponse response)
        {
            ISession session = DefaultSession.Instance; // we don't have to alloc/dealloc memory parts which we never change

            // user will be set by Authentication middleware, we don't do anything here
            return new MiniAppContext(connectionContext, app, request, response, session, null);
        }

        private static HttpVersions TryGetHttpVersion(ReadOnlySequence<byte> buffer)
        {
            var httpVersion = HttpVersions.Http11;

            if (buffer.Length >= HTTP2_MAGIC.Length)
            {
                var span = ToSpan(buffer.Slice(0, HTTP2_MAGIC.Length));

                if (span.SequenceEqual(HTTP2_MAGIC))
                {
                    return HttpVersions.Http20;
                }
            }

            return httpVersion;
        }

        private static ReadOnlySpan<byte> ToSpan(ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsSingleSegment)
            {
                return buffer.FirstSpan;
            }
            return buffer.ToArray();
        }
    }
}
