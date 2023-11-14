﻿namespace MiniWebServer.Server.Abstractions
{
    public interface IProtocolHandlerStorage
    {
        // we write to OtputStream when we build requests and send InputStream to request handlers
        Stream GetWriter();
        Stream GetReader();
    }
}
