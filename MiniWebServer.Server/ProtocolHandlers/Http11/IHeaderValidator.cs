﻿namespace MiniWebServer.Server.ProtocolHandlers.Http11
{
    public interface IHeaderValidator
    {
        bool Validate(string name, string value);
    }
}
