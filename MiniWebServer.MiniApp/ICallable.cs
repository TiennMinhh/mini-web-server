﻿namespace MiniWebServer.MiniApp
{
    /// <summary>
    /// a ICallable is a resource which you can call and make a response based on that, we don't care what it is, it is just an abstract thing
    /// An implementation can be a PHP engine, a javascript engine or anything else...
    /// We don't have to apply abstraction to a deeper level since HTTP is a standard and we all know there will be no other methods except what we have 
    /// defined (in the standard)
    /// </summary>
    public interface ICallable
    {
        Task InvokeAsync(IMiniAppContext context, CancellationToken cancellationToken = default);
    }

    public delegate Task RequestDelegate(IMiniAppContext context, CancellationToken cancellationToken = default);

}
