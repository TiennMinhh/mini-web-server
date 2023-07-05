﻿using MiniWebServer.Abstractions.Http.Header;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.Abstractions.Http
{
    // here is the this of standard request headers defined in 
    public class HttpRequestHeaders: HttpHeaders
    {
        public HttpRequestHeaders() {
            HeaderAdded += HttpRequestHeaders_AddedOrModified;
            HeaderChanged += HttpRequestHeaders_AddedOrModified;
            HeaderRemoved += HttpRequestHeaders_HeaderRemoved;
        }

        private void HttpRequestHeaders_HeaderRemoved(HttpHeader header)
        {
            if (header.Name == "Range")
            {
                Range = null;
            }
        }

        private void HttpRequestHeaders_AddedOrModified(HttpHeader header)
        {
            if (header == null)
                return;

            if (header.Name == "Range")
            {
                var value = header.Value.FirstOrDefault();
                if (value != null && RangeHeader.TryParse(value, out var range))
                {
                    this.Range = range;
                }
                else
                {
                    throw new InvalidOperationException("Invalid Range header");
                }
            }
        }

        public string AcceptLanguage
        {
            get
            {
                return TryGetValueAsString("Accept-Language");
            }
        }
        public string Authorization
        {
            get
            {
                return TryGetValueAsString("Authorization");
            }
        }
        public string CacheControl
        {
            get
            {
                return TryGetValueAsString("Cache-Control");
            }
        }
        public string Connection
        {
            get
            {
                return TryGetValueAsString("Connection");
            }
        }
        public string ContentType
        {
            get
            {
                return TryGetValueAsString("Content-Type");
            }
        }
        public string Host
        {
            get
            {
                return TryGetValueAsString("Host");
            }
        }
        public string TransferEncoding
        {
            get
            {
                return TryGetValueAsString("Transfer-Encoding");
            }
        }

        public string[] AcceptEncoding
        {
            get
            {
                var value = TryGetValueAsString("Accept-Encoding");
                if (string.IsNullOrEmpty(value))
                {
                    return Array.Empty<string>();
                }
                // todo: need to support something like: br;q=1.0, gzip;q=0.8, *;q=0.1
                var values = value.Split(", ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                return values;
            }
        }

        public RangeHeader? Range { get; private set; }

        private string TryGetValueAsString(string name, string defaultValue = "")
        {
            if (TryGetValue(name, out var value))
            {
                if (value == null)
                    return defaultValue;

                return value.Value.FirstOrDefault(defaultValue);
            }
            else
            {
                return defaultValue;
            }

        }
    }
}
