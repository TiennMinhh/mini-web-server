﻿using Microsoft.Extensions.Logging;
using MiniWebServer.Abstractions;
using MiniWebServer.Abstractions.Http;
using MiniWebServer.Server.Abstractions;
using MiniWebServer.Server.Abstractions.Http;
using MiniWebServer.Server.Abstractions.Parsers;
using MiniWebServer.Server.Abstractions.Parsers.Http11;
using MiniWebServer.Server.Http;
using MiniWebServer.Server.ProtocolHandlers.Http11;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using HttpMethod = MiniWebServer.Abstractions.Http.HttpMethod;

namespace MiniWebServer.Server.ProtocolHandlers.Http2
{
    /* 
     * protocol handler for http2 based on RFC 9113 (https://datatracker.ietf.org/doc/html/rfc9113)
     * 
     * Some problems we must solve before going online:
     * - find a way to switch between HTTP11 and HTTP2 (will it be handled in protocol handler factory in or internally inside HTTP2 protocol handler?)
     * - how do we handle frame buffers effectively? will it need to be shared between threads?
     * - a buffer will be controlled inside or outside protocol handlers?
     * 
     * Note:
     * - use buffer pools 
     * 
     */

    public partial class Http2ProtocolHandler(ILoggerFactory loggerFactory, IHttpComponentParser httpComponentParser, ICookieValueParser cookieValueParser) : IProtocolHandler
    {
        private const int DefaultMaxFrameSize = 16384; // frame size = 2^14 unless when you change it with a SETTINGS frame

        private readonly ILoggerFactory loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        private readonly ILogger<Http2ProtocolHandler> logger = loggerFactory.CreateLogger<Http2ProtocolHandler>();

        public int ProtocolVersion => 20;
        private int maxFrameSize = DefaultMaxFrameSize;
        private readonly Http2StreamContainer streamContainer = [];
        private ulong frameCount = 0Lu;

        // the following settings are controlled by frames sent by client
        private uint windowSizeIncrement;

        public Task ReadBodyAsync(PipeReader reader, IHttpRequest requestBuilder, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ReadRequestAsync(PipeReader reader, IHttpRequestBuilder requestBuilder, CancellationToken cancellationToken)
        {
            try
            {
                ReadResult readResult = await reader.ReadAsync(cancellationToken);
                ReadOnlySequence<byte> buffer = readResult.Buffer;

                //requestBuilder.SetBodyPipeline(new Pipe());

                var httpMethod = HttpMethod.Get;

                var frame = new Http2Frame(); // can we reuse?

                // HTTP2 connections always start with a SETTINGS frame
                while (Http2FrameReader.TryReadFrame(ref buffer, ref frame, maxFrameSize, out ReadOnlySequence<byte> payload))
                {
                    if (logger.IsEnabled(LogLevel.Debug))
                    {
                        logger.LogDebug("Frame found: {f}, Stream Id: {sid}, payload length: {pll}", frame.FrameType, frame.StreamIdentifier, frame.Length);
                    }

                    if (frameCount == 0 && frame.FrameType != Http2FrameType.SETTINGS)
                    {
                        logger.LogError("First frame must be a SETTINGS frame");
                        return false;
                    }

                    if (!ProcessFrame(ref frame, ref payload, logger)) { 
                        return false; 
                    }

                    frameCount = Interlocked.Increment(ref frameCount);
                }

                return false;
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Connection expired");
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading request");
                return false;
            }
        }

        private bool ProcessFrame(ref Http2Frame frame, ref ReadOnlySequence<byte> payload, ILogger logger)
        {
            var b = false;

            switch (frame.FrameType)
            {
                case Http2FrameType.HEADERS:
                    b = ProcessHEADERSFrame(ref frame, ref payload);
                    break;
                case Http2FrameType.SETTINGS:
                    b = ProcessSETTINGSFrame(ref frame, ref payload);
                    break;
                case Http2FrameType.WINDOW_UPDATE:
                    b = ProcessWINDOW_UPDATEFrame(ref frame, ref payload);
                    break;
                default:
                    logger.LogError("Unknown frame type: {ft}, stream: {sid}", frame.FrameType, frame.StreamIdentifier);
                    break;
            }

            return b;
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public Task<bool> WriteResponseAsync(IHttpResponse response, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
