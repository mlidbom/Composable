

// ReSharper disable MemberCanBeMadeStatic.Global

using Composable;
using Composable.Messaging;

namespace AccountManagement.API
{
    public static class AccountApi
    {
        public static readonly StartResourceQuery Start = new StartResourceQuery();
    }

    [TypeId("16FF2F11-ACF2-4813-8B4F-EF6D89994C6C")]public class StartResourceQuery : IQuery<StartResource> {}
}
