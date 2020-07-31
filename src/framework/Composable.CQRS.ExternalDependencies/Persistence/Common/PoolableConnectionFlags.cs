using System;

namespace Composable.Persistence.Common
{
    [Flags] enum PoolableConnectionFlags
    {
        Defaults = 0,
        MustUseSameConnectionThroughoutATransaction = 1
    }
}
