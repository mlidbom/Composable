using System;
using System.Web;

namespace Composable.System.Web
{
    class HttpRequestIdFetcher : IHttpRequestIdFetcher {
        public Guid GetCurrent()
        {
            return HttpContext.Current.Request.Id();
        }
    }
}