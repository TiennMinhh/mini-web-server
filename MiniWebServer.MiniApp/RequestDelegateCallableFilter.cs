﻿namespace MiniWebServer.MiniApp
{
    internal class RequestDelegateCallableFilter : ICallableFilter
    {
        private readonly Func<IMiniAppContext, CancellationToken, bool> filter;

        public RequestDelegateCallableFilter(Func<IMiniAppContext, CancellationToken, bool> filter)
        {
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter));
        }

        public async Task<bool> InvokeAsync(IMiniAppContext context, CancellationToken cancellationToken)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            return await Task.FromResult(filter(context, cancellationToken));
        }
    }
}
