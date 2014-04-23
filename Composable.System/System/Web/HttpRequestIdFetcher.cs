using System;
using System.Web;

namespace Composable.System.Web
{
    ///<summary>Default implementation of <see cref="IHttpRequestIdFetcher"/>. Fetches/assignes the id using an entry in HttpContext.Current.Request.RequestContext.HttpContext.Items </summary>
    public class HttpRequestIdFetcher : IHttpRequestIdFetcher 
    {
        ///<summary>Gets the id for the current request</summary>
        public Guid GetCurrent()
        {
            return HttpContext.Current.Request.Id();
        }
    }
}