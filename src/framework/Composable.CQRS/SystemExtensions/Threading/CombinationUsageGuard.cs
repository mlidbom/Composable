namespace Composable.SystemExtensions.Threading {
    class CombinationUsageGuard : ISingleContextUseGuard
    {
        ISingleContextUseGuard[] _usageGuards;
        public CombinationUsageGuard(params ISingleContextUseGuard[] usageGuards) => _usageGuards = usageGuards;
        public void AssertNoContextChangeOccurred(object guarded)
        {
            foreach(var guard in _usageGuards)
            {
                guard.AssertNoContextChangeOccurred(guarded);
            }
        }
    }
}