using System;

namespace Composable.CQRS.Windsor.Testing
{
    [Obsolete("This inteface is obsolete and will soon be removed. Please use Composable.Windsor.Testing instead. Search and replace 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;'")]
    public interface IConfigureWiringForTests
    {
        void ConfigureWiringForTesting();
    }
}