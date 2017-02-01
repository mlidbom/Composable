using System;
using System.Diagnostics.Contracts;

namespace Composable.SystemExtensions.Threading
{
    ///<summary>Base class that takes care of most of the complexity of implementing <see cref="ISingleContextUseGuard"/></summary>
    public abstract class UsageGuard : ISingleContextUseGuard
    {
        [ThreadStatic] static bool _isInIgnoredContextDueToInfrastructureSuchAsTransaction;

        static bool IsInIgnoredContextDueToInfrastructureSuchAsTransaction
        {
            get { return _isInIgnoredContextDueToInfrastructureSuchAsTransaction; } 
            set { _isInIgnoredContextDueToInfrastructureSuchAsTransaction = value; }
        }

        ///<summary>Occasionally you have to be able to run code without validating the context. Passing such code to this method allows for that.</summary>
        public static void RunInContextExcludedFromSingleUseRule(Action action)
        {
            Contract.Requires(action != null);
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


        ///<summary>Implementations throw an exception if the context has changed.</summary>
        public void AssertNoContextChangeOccurred(object guarded)
        {
            if(IsInIgnoredContextDueToInfrastructureSuchAsTransaction)                
            {
                return;                
            }
            InternalAssertNoChangeOccurred(guarded);
        }

        ///<summary>Implemented by inheritors to do the actual check for any context changes. Implementations throw an exception if the context has changed.</summary>
        protected abstract void InternalAssertNoChangeOccurred(object guarded);
    }
}