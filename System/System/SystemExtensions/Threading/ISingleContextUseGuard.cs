namespace Composable.SystemExtensions.Threading
{
    public interface ISingleContextUseGuard
    {
        void AssertNoContextChangeOccurred(object guarded);
    }
}