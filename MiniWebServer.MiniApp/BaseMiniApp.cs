﻿using Microsoft.Extensions.DependencyInjection;

namespace MiniWebServer.MiniApp
{
    public class BaseMiniApp : IMiniApp
    {
        // todo: typical web apps will have may be hundred of routes, so this route table should be optimized for searching (can we use a B-Tree here?)
        // (update after checking https://github.com/microsoft/referencesource/blob/master/mscorlib/system/collections/generic/dictionary.cs#L297)
        // Dictionary is based on checksum and it is good enough to use here, we just need to partition by method (or may be by request segments also?)

        private readonly IDictionary<string, CallableActionDelegate> callableEndpoints = new Dictionary<string, CallableActionDelegate>();
        private readonly ServiceProvider services;
        private readonly IEnumerable<IMiddleware> middlewareChain;

        public BaseMiniApp(ServiceProvider services, IEnumerable<IMiddleware> middlewareChain)
        {
            this.services = services ?? throw new ArgumentNullException(nameof(services));
            this.middlewareChain = middlewareChain ?? throw new ArgumentNullException(nameof(middlewareChain));
        }

        public ICallableBuilder Map(string route, ICallable action, params Abstractions.Http.HttpMethod[] methods)
        {
            if (!methods.Any())
            {
                throw new ArgumentException("Methods required", nameof(methods));
            }

            var r = new CallableActionDelegate(route, action, methods);

            if (callableEndpoints.ContainsKey(route))
                callableEndpoints[route] = r;
            else
                callableEndpoints.Add(route, r);

            return r;
        }

        public virtual ICallable? Find(IMiniAppRequestContext context)
        {
            ICallable callable = this;

            foreach (var action in callableEndpoints.Values)
            {
                if (action.IsMatched(context.Request.Url, context.Request.Method))
                {
                    callable = new ActionDelegateCallable(action, callable);
                    break; // we need only one endpoint
                }
            }

            foreach (var middleware in middlewareChain)
            {
                var callWrapper = new MiddlewareWrapper(middleware, callable);

                callable = callWrapper;
            }

            return callable;
        }

        public Task InvokeAsync(IMiniAppRequestContext context, CancellationToken cancellationToken = default)
        {
            context.Response.StatusCode = Abstractions.HttpResponseCodes.NotFound;

            return Task.CompletedTask;
        }

        private class MiddlewareWrapper : ICallable
        {
            public MiddlewareWrapper(IMiddleware middleware, ICallable next)
            {
                Middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));
                Next = next ?? throw new ArgumentNullException(nameof(next));
            }

            public IMiddleware Middleware { get; init; }
            public ICallable Next { get; init; }

            public Task InvokeAsync(IMiniAppRequestContext context, CancellationToken cancellationToken = default)
            {
                return Middleware.InvokeAsync(context, Next, cancellationToken);
            }
        }
    }
}
