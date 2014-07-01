using System;
using System.Diagnostics.Contracts;
using Composable.SystemExtensions.Threading;

namespace Composable.System.Web
{
    ///<summary>Ensures that the monitored component is only used within a single http request</summary>
    public class SingleHttpRequestUseGuard : UsageGuard
    {
        private readonly IHttpRequestIdFetcher _httpRequestIdFetcher;
        private readonly Guid _initialRequestId;

        ///<summary>Construct an instance bound to the currently executing http request. The id of the current request will be extracted using the supplied <see cref="IHttpRequestIdFetcher"/></summary>
        public SingleHttpRequestUseGuard(IHttpRequestIdFetcher httpRequestIdFetcher)
        {
            Contract.Requires(httpRequestIdFetcher != null);

            _httpRequestIdFetcher = httpRequestIdFetcher;
            _initialRequestId = httpRequestIdFetcher.GetCurrent();
        }

        ///<summary>Throws an exception if the request has changed since the guard was created.</summary>
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