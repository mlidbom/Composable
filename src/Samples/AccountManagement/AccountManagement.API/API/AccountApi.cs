

// ReSharper disable MemberCanBeMadeStatic.Global

using System;
using Composable.Messaging;

namespace AccountManagement.API
{
    public static class AccountApi
    {
        public static readonly NewableSingletonQuery<StartResource> Start = SingletonQuery.NewableFor<StartResource>();
    }
}
