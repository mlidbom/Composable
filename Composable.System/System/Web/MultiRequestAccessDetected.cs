using System;

namespace Composable.System.Web
{
    ///<summary>Thrown if a the current request has changed for a component guarded by a <see cref="SingleHttpRequestUseGuard"/> </summary>
    public class MultiRequestAccessDetected : Exception
    {
        ///<summary>Creates an instance with the supplied information.</summary>
        public MultiRequestAccessDetected(object guarded, Guid initialRequestId, Guid currentRequestId) : base(
            string.Format("Atttempt to use {0} from request:{1}, when owning request was : {2}",
                          guarded,
                          currentRequestId,
                          initialRequestId
                )) {}
    }
}