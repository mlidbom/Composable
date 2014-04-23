using System;
using System.Diagnostics.Contracts;
using System.Web;

namespace Composable.System.Web
{
    ///<summary>Adds extension that assigns and tracks a unique Guid to each HttpRequest</summary>
    public static class HttpRequestIdSupplier
    {
        private const string UniqueRequestId = "__Unique_Request_Id";

        ///<summary>Returns the unique Guid of the current request</summary>
        public static Guid Id(this HttpRequest me)
        {
            Contract.Requires(me != null && me.RequestContext != null);

            if(!me.RequestContext.HttpContext.Items.Contains(UniqueRequestId))
            {
                me.RequestContext.HttpContext.Items.Add(UniqueRequestId, Guid.NewGuid());
            }
            return (Guid)me.RequestContext.HttpContext.Items[UniqueRequestId];
        }
    }
}