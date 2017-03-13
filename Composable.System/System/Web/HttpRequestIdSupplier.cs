using System;

using System.Web;
using Composable.Contracts;

namespace Composable.System.Web
{
    ///<summary>Adds extension that assigns and tracks a unique Guid to each HttpRequest</summary>
    static class HttpRequestIdSupplier
    {
        const string UniqueRequestId = "__Unique_Request_Id";

        ///<summary>Returns the unique Guid of the current request</summary>
        public static Guid Id(this HttpRequest me)
        {
            ContractOptimized.Argument(me, nameof(me))
                             .NotNull();

            Contract.Assert(me.RequestContext != null, "me.RequestContext != null");

            if(!me.RequestContext.HttpContext.Items.Contains(UniqueRequestId))
            {
                me.RequestContext.HttpContext.Items.Add(UniqueRequestId, Guid.NewGuid());
            }
            return (Guid)me.RequestContext.HttpContext.Items[UniqueRequestId];
        }
    }
}