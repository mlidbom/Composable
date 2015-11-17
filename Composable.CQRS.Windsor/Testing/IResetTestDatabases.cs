using System;

namespace Composable.CQRS.Windsor.Testing
{
    [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
    public interface IResetTestDatabases
    {
        void ResetDatabase();
    }
}