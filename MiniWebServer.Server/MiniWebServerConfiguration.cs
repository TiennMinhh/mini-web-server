﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace MiniWebServer.Server
{
    public class MiniWebServerConfiguration
    {

        public List<MiniWebServerBindingConfiguration> Bindings = new();
        public List<HostConfiguration> Hosts = new();

        // advanced settings
        public int ReadBufferSize { get; init; } = 1024 * 8;
        public long MaxRequestBodySize { get; set; } = 1024 * 1024 * 10; // 10MB 
        public int ReadRequestTimeout { get; set; } = 2 * 1000; // 2 seconds are enough to read any request headers
        public int SendResponseTimeout { get; set; } = 300000;
        public int ConnectionTimeout { get; set; } = 180000;
    }
}
