using System;

namespace Composable.System.Web
{
    ///<summary>Fetches a unique Guid associated with the current http request.</summary>
    public interface IHttpRequestIdFetcher
    {
        ///<summary>Gets the id for the current request</summary>
        Guid GetCurrent();
    }
}