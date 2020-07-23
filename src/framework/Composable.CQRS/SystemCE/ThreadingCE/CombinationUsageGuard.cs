namespace Composable.SystemCE.ThreadingCE {
    class CombinationUsageGuard : ISingleContextUseGuard
    {
        readonly ISingleContextUseGuard[] _usageGuards;
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