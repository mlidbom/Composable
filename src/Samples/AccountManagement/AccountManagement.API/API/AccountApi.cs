

// ReSharper disable MemberCanBeMadeStatic.Global

using Composable.Messaging.Buses;

namespace AccountManagement.API
{
    public static class AccountApi
    {
        public static readonly SelfGeneratingResourceQuery<StartResource> Start = SelfGeneratingResourceQuery<StartResource>.Instance;
    }
}
