using System;
using System.Diagnostics.Contracts;
using System.Web;

namespace Composable.System.Web
{
    public static class HttpRequestExtensions
    {
        private const string UniqueReuestId = "__Unique_Request_Id";

        public static Guid Id(this HttpRequest me)
        {
            Contract.Requires(me != null);

            if(!me.RequestContext.HttpContext.Items.Contains(UniqueReuestId))
            {
                me.RequestContext.HttpContext.Items.Add(UniqueReuestId, Guid.NewGuid());
            }
            return (Guid)me.RequestContext.HttpContext.Items[UniqueReuestId];
        }
    }
}