﻿using Microsoft.Extensions.Logging;
using MiniWebServer.Abstractions;
using MiniWebServer.MiniApp;
using HttpMethod = global::MiniWebServer.Abstractions.Http.HttpMethod;
using System.IO;
using MiniWebServer.Server.Abstractions;
using Microsoft.Extensions.Logging.Abstractions;

namespace MiniWebServer.StaticFiles
{
    internal class StaticFilesMiddleware : IMiddleware
    {
        private readonly ILogger<StaticFilesMiddleware> logger;
        private readonly DirectoryInfo directoryInfo;
        private readonly IMimeTypeMapping mimeTypeMapping;
        private readonly StaticFilesOptions options;
        private readonly string[] defaultDocuments;

        public StaticFilesMiddleware(StaticFilesOptions options, IMimeTypeMapping? mimeTypeMapping, ILoggerFactory? loggerFactory)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(mimeTypeMapping);

            directoryInfo = new DirectoryInfo(options.Root);
            this.defaultDocuments = defaultDocuments ?? Array.Empty<string>();
            this.mimeTypeMapping = mimeTypeMapping;
            this.options = options;

            logger = loggerFactory != null ? loggerFactory.CreateLogger<StaticFilesMiddleware>() : NullLogger<StaticFilesMiddleware>.Instance;
        }
        public async Task InvokeAsync(IMiniAppContext context, ICallable next, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(next);

            string url = context.Request.Url;

            if (directoryInfo.Exists)
            {
                if (url.StartsWith("/"))
                {
                    url = url[1..];
                }
                url = url.Replace('/', Path.DirectorySeparatorChar);

                FileInfo? file = null;
                if (string.IsNullOrEmpty(url) && defaultDocuments.Any())
                {
                    foreach (var document in defaultDocuments)
                    {
                        FileInfo f = new(Path.Combine(directoryInfo.FullName, document));
                        if (f.Exists)
                        {
                            file = f;
                            break;
                        }
                    }
                }
                else
                {
                    file = new(Path.Combine(directoryInfo.FullName, url));
                    if (!file.Exists)
                        file = null;
                }

                if (file != null)
                {
                    if (context.Request.Method == HttpMethod.Get)
                    {
                        try
                        {
                            string fileExt = file.Extension;
                            string mimeType = mimeTypeMapping.GetMimeMapping(fileExt);

                            context.Response.Headers.ContentType = mimeType;
                            context.Response.Content = new MiniApp.Content.FileContent(file);

                            //if (IsText(mimeType))
                            //{
                            //    using var reader = file.OpenText();
                            //    var content = await reader.ReadToEndAsync(cancellationToken);

                            //    context.Response.AddHeader("Content-Type", mimeType);
                            //    context.Response.AddHeader("Content-Length", content.Length.ToString());
                            //    context.Response.SetContent(new MiniApp.Content.StringContent(content));
                            //}
                            //else
                            //{
                            //    using var stream = file.OpenRead();
                            //    var length = file.Length;
                            //    var content = new byte[length];

                            //    stream.Read(content, 0, content.Length);

                            //    context.Response.AddHeader("Content-Type", mimeType);
                            //    context.Response.AddHeader("Content-Length", content.Length.ToString());
                            //    context.Response.SetContent(new MiniApp.Content.ByteArrayContent(content));
                            //}

                            if (options.CacheOptions.MaxAge > 0)
                            {
                                long maxAge = options.CacheOptions.MaxAge;

                                if (maxAge > 0)
                                {
                                    context.Response.Headers.Add("Cache-Control", $"max-age={maxAge}");
                                }
                                else
                                {
                                    context.Response.Headers.Add("Cache-Control", "no-cache"); // no-cache doesn't mean 'don't cache'
                                }
                            }

                            context.Response.StatusCode = HttpResponseCodes.OK;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, message: null);
                            context.Response.StatusCode = HttpResponseCodes.InternalServerError;
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = HttpResponseCodes.MethodNotAllowed;
                    }
                }
                else
                {
                    // instead of returning a 404 Not Found, we pass the request to next middleware
                    await next.InvokeAsync(context, cancellationToken);
                }
            }
            else
            {
                // instead of returning a 404 Not Found, we pass the request to next middleware
                await next.InvokeAsync(context, cancellationToken);
            }
        }

        private static bool IsText(string mimeType) => mimeType.StartsWith("text/");
    }
}