using System;
using System.Web;
using JetBrains.Annotations;

namespace Composable.System.Web
{
    ///<summary>Default implementation of <see cref="IHttpRequestIdFetcher"/>. Fetches/assignes the id using an entry in HttpContext.Current.Request.RequestContext.HttpContext.Items </summary>
    [UsedImplicitly] public class HttpRequestIdFetcher : IHttpRequestIdFetcher 
    {
        ///<summary>Gets the id for the current request</summary>
        public Guid GetCurrent()
        {
            return HttpContext.Current.Request.Id();
        }
    }
}