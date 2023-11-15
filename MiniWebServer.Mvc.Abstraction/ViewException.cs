﻿using System.Runtime.Serialization;

namespace MiniWebServer.Mvc.Abstraction
{
    public class ViewException : Exception
    {
        public ViewException()
        {
        }

        public ViewException(string? message) : base(message)
        {
        }

        public ViewException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
