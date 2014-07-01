using System.Diagnostics.Contracts;

namespace Composable.SystemExtensions.Threading
{    
 
    ///<summary>Implementations ensure that a component is only used within the allowed context. Such as a single thread, single http request etc.</summary>
    [ContractClass(typeof(ISingleContextUseGuardContract))]
    public interface ISingleContextUseGuard
    {
        ///<summary>Implementations throw an exception if the context has changed.</summary>
        void AssertNoContextChangeOccurred(object guarded);
    }

    [ContractClassFor(typeof(ISingleContextUseGuard))]
    // ReSharper disable once InconsistentNaming
    abstract class ISingleContextUseGuardContract : ISingleContextUseGuard
    {
        public void AssertNoContextChangeOccurred(object guarded)
        {
            Contract.Requires(guarded != null);
        }
    }
}