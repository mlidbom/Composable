using System;

namespace Composable.System.Web
{
    public class MultiRequestAccessDetected : Exception
    {
        public MultiRequestAccessDetected(object guarded, Guid initialRequestId, Guid currentRequestId) : base(
            string.Format("Atttempt to use {0} from request:{1}, when owning request was : {2}",
                          guarded,
                          currentRequestId,
                          initialRequestId
                )) {}
    }
}