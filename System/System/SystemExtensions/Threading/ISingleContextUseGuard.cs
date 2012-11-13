namespace Composable.SystemExtensions.Threading
{
    public interface ISingleContextUseGuard
    {
        void AssertNoThreadChangeOccurred(object guarded);
    }
}