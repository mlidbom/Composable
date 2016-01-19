using System;

namespace Composable.Windsor.Testing
{
    [Obsolete("Relying on this method is fragile and apt to cause trouble. You should make sure to set up a new container for each test instead. That is far more safe and simple than using this")]
    public interface IResetTestDatabases
    {
        void ResetDatabase();
    }
}