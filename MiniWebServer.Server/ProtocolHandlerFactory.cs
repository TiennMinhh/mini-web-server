﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniWebServer.HttpParser.Http11;
using MiniWebServer.Server.Abstractions;
using MiniWebServer.Server.Abstractions.Parsers;
using MiniWebServer.Server.Abstractions.Parsers.Http11;
using MiniWebServer.Server.Cookie;
using MiniWebServer.Server.ProtocolHandlers.Http11;

namespace MiniWebServer.Server
{
    public class ProtocolHandlerFactory(ILoggerFactory loggerFactory, IServiceProvider services) : IProtocolHandlerFactory
    {
        public const int HTTP11 = 101;
        private readonly ILoggerFactory loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        private readonly IServiceProvider services = services ?? throw new ArgumentNullException(nameof(services));

        public IProtocolHandler Create(ProtocolHandlerConfiguration config)
        {
            if (config.ProtocolVersion == HTTP11)
            {
                // in reality we often use the default parsers

                return new Http11ProtocolHandler(config, loggerFactory,
                    services.GetService<IHttpComponentParser>() ?? new ByteSequenceHttpParser(loggerFactory),
                    services.GetService<ICookieValueParser>() ?? new DefaultCookieParser()
                    );
            }

            throw new ArgumentOutOfRangeException(nameof(config.ProtocolVersion), "Unknown protocol version");
        }
    }
}
