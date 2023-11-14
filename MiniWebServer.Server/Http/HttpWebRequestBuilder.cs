﻿using MiniWebServer.Abstractions.Http;
using MiniWebServer.Server.Abstractions.Http;
using System.IO.Pipelines;
using HttpMethod = MiniWebServer.Abstractions.Http.HttpMethod;

namespace MiniWebServer.Server.Http
{
    public class HttpWebRequestBuilder : IHttpRequestBuilder
    {
        private HttpMethod httpMethod = HttpMethod.Get;
        private readonly HttpRequestHeaders headers = new();
        private readonly List<HttpTransferEncoding> transferEncodings = new();
        private readonly List<HttpCookie> cookies = new();
        private string host = string.Empty;
        private int port = 80;
        private string url = "/";
        private string queryString = string.Empty;
        private string hash = string.Empty;
        private Pipe? bodyPipeline;
        private long contentLength;
        private string contentType = string.Empty;
        private string[] segments = Array.Empty<string>();
        private ulong requestId;
        private bool isHttps = false;
        private readonly HttpParameters parameters = new();

        public IHttpRequestBuilder AddHeader(string name, string value)
        {
            headers.Add(name, value);

            return this;
        }

        public IHttpRequestBuilder AddHeaders(IEnumerable<KeyValuePair<string, string>> keyValues)
        {
            foreach (var k in headers)
            {
                headers.Add(k.Key, k.Value);
            }

            return this;
        }

        public HttpRequest Build()
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new InvalidOperationException("host cannot be empty");
            }

            var request = new HttpRequest(
                requestId,
                httpMethod,
                host,
                port,
                url,
                headers,
                queryString,
                hash,
                parameters,
                segments,
                new HttpCookies(cookies),
                bodyPipeline ?? new Pipe(),
                contentLength,
                contentType,
                isHttps
                );

            return request;
        }

        public IHttpRequestBuilder SetMethod(HttpMethod method)
        {
            httpMethod = method;

            return this;
        }
        public IHttpRequestBuilder SetPort(int port)
        {
            if (port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            this.port = port;

            return this;
        }
        public IHttpRequestBuilder SetHost(string host)
        {
            this.host = host;

            return this;
        }

        public IHttpRequestBuilder SetUrl(string url)
        {
            this.url = url;

            return this;
        }

        public IHttpRequestBuilder AddTransferEncoding(string transferEncodings)
        {
            string[] encodings = transferEncodings.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            foreach (var en in encodings)
            {
                var encoding = HttpTransferEncoding.GetEncoding(en);

                if (encoding == HttpTransferEncoding.Unknown)
                    throw new InvalidOperationException($"Unknown transfer encoding: {transferEncodings}");
                else
                    this.transferEncodings.Add(encoding);
            }

            return this;
        }

        public IHttpRequestBuilder SetParameters(HttpParameters parameters)
        {
            foreach (var parameter in parameters)
            {
                this.parameters.Add(parameter.Key, parameter.Value);
            }

            return this;
        }

        public IHttpRequestBuilder SetQueryString(string queryString)
        {
            this.queryString = queryString;

            return this;
        }

        public IHttpRequestBuilder SetHash(string hash)
        {
            this.hash = hash;

            return this;
        }

        public IHttpRequestBuilder SetBodyPipeline(Pipe bodyPipeline)
        {
            this.bodyPipeline = bodyPipeline;

            return this;
        }

        public IHttpRequestBuilder AddCookie(IEnumerable<HttpCookie> cookies)
        {
            this.cookies.AddRange(cookies);

            return this;
        }

        public IHttpRequestBuilder SetContentLength(long contentLength)
        {
            this.contentLength = contentLength;

            return this;
        }

        public IHttpRequestBuilder SetContentType(string contentType)
        {
            this.contentType = contentType;

            return this;
        }

        public IHttpRequestBuilder SetSegments(string[] segments)
        {
            this.segments = segments;

            return this;
        }

        public IHttpRequestBuilder SetRequestId(ulong requestId)
        {
            this.requestId = requestId;

            return this;
        }

        public IHttpRequestBuilder SetHttps(bool isHttps)
        {
            this.isHttps = isHttps;

            return this;
        }
    }
}
