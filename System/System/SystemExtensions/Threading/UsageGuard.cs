using System;

namespace Composable.SystemExtensions.Threading
{
    public abstract class UsageGuard : ISingleContextUseGuard
    {
        [ThreadStatic]
        private static bool _isInIgnoredContextDueToInfrastructureSuchAsTransaction;
        protected static bool IsInIgnoredContextDueToInfrastructureSuchAsTransaction
        {
            get { return _isInIgnoredContextDueToInfrastructureSuchAsTransaction; }
            private set { _isInIgnoredContextDueToInfrastructureSuchAsTransaction = value; }
        }

        public static void RunInContextExcludedFromSingleUseRule(Action action)
        {
            //Make sure that we do not loose the context if this is called again from within such a context
            if(IsInIgnoredContextDueToInfrastructureSuchAsTransaction)
            {
                action();
                return;
            }

            try
            {
                IsInIgnoredContextDueToInfrastructureSuchAsTransaction = true;
                action();
            }
            finally
            {
                IsInIgnoredContextDueToInfrastructureSuchAsTransaction = false;
            }
        }


        public void AssertNoContextChangeOccurred(object guarded)
        {
            if(IsInIgnoredContextDueToInfrastructureSuchAsTransaction)                
            {
                return;                
            }
            InternalAssertNoChangeOccurred(guarded);
        }

        protected abstract void InternalAssertNoChangeOccurred(object guarded);
    }
}