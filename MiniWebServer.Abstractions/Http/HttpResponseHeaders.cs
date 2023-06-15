﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.Abstractions.Http
{
    public class HttpResponseHeaders: HttpHeaders
    {
        public string ContentLength
        {
            get
            {
                return TryGetValueAsString("Content-Length");
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
        public string ContentEncoding
        {
            get
            {
                return TryGetValueAsString("Content-Encoding");
            }
        }
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
