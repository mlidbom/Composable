using System;
using System.Diagnostics.Contracts;
using Composable.SystemExtensions.Threading;

namespace Composable.System.Web
{
    public class SingleHttpRequestUseGuard : UsageGuard
    {
        private readonly IHttpRequestIdFetcher _httpRequestIdFetcher;
        private readonly Guid _initialRequestId;

        public SingleHttpRequestUseGuard(IHttpRequestIdFetcher httpRequestIdFetcher)
        {
            Contract.Requires(httpRequestIdFetcher != null);

            _httpRequestIdFetcher = httpRequestIdFetcher;
            _initialRequestId = httpRequestIdFetcher.GetCurrent();
        }

        override protected void InternalAssertNoChangeOccurred(object guarded)
        {
            var currentRequestId = _httpRequestIdFetcher.GetCurrent();
            if(_initialRequestId != currentRequestId)
            {
                throw new MultiRequestAccessDetected(guarded, _initialRequestId, currentRequestId);
            }
        }
    }
}