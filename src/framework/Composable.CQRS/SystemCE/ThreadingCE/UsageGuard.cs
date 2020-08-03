namespace Composable.SystemCE.ThreadingCE
{
    ///<summary>Base class that takes care of most of the complexity of implementing <see cref="ISingleContextUseGuard"/></summary>
    abstract class UsageGuard : ISingleContextUseGuard
    {
 ///<summary>Implementations throw an exception if the context has changed.</summary>
        public void AssertNoContextChangeOccurred(object guarded) => InternalAssertNoChangeOccurred(guarded);

 ///<summary>Implemented by inheritors to do the actual check for any context changes. Implementations throw an exception if the context has changed.</summary>
        protected abstract void InternalAssertNoChangeOccurred(object guarded);
    }
}