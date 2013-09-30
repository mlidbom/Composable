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