

// ReSharper disable MemberCanBeMadeStatic.Global

using Composable.Messaging;

namespace AccountManagement.API
{
    public static class AccountApi
    {
        public static readonly SingletonQuery<StartResource> Start = SingletonQuery.For<StartResource>();
    }
}
