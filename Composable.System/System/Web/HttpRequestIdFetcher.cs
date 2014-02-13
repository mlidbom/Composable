using System;
using System.Web;

namespace Composable.System.Web
{
    public class HttpRequestIdFetcher : IHttpRequestIdFetcher {
        public Guid GetCurrent()
        {
            return HttpContext.Current.Request.Id();
        }
    }
}