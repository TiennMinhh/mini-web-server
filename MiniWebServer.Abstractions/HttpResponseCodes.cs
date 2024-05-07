﻿namespace MiniWebServer.Abstractions
{
    public enum HttpResponseCodes
    {
        SwitchingProtocols = 101,
        OK = 200,
        NoContent = 204,
        PartialContent = 206,
        MovedPermanently = 301,
        Found = 302,
        NotModified = 304,
        TemporaryRedirect = 307,
        PermanentRedirect = 308,
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        MethodNotAllowed = 405,
        InternalServerError = 500,
        NotImplemented = 501,
    }
}
