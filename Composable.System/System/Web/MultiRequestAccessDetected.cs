using System;

namespace Composable.System.Web
{
    ///<summary>Thrown if a the current request has changed for a component guarded by a <see cref="SingleHttpRequestUseGuard"/> </summary>
    class MultiRequestAccessDetected : Exception
    {
        ///<summary>Creates an instance with the supplied information.</summary>
        public MultiRequestAccessDetected(object guarded, Guid initialRequestId, Guid currentRequestId) : base(
                                                                                                               $"Atttempt to use {guarded} from request:{currentRequestId}, when owning request was : {initialRequestId}") {}
    }
}