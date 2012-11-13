using System;

namespace Composable.System.Web
{
    public interface IHttpRequestIdFetcher
    {
        Guid GetCurrent();
    }
}