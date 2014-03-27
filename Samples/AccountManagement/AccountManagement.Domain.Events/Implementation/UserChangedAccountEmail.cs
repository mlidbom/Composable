using System;
using AccountManagement.Domain.Shared;
using Composable.CQRS.EventSourcing;
using JetBrains.Annotations;

namespace AccountManagement.Domain.Events.Implementation
{
    public class UserChangedAccountEmailEvent : AggregateRootEvent, IUserChangedAccountEmailEvent
    {
        [Obsolete("NServicebus requires this constructor to exist.", true), UsedImplicitly] //ncrunch: no coverage
        public UserChangedAccountEmailEvent() {} //ncrunch: no coverage

        public UserChangedAccountEmailEvent(Email email)
        {
            Email = email;
        }

        public Email Email { get; private set; }
    }
}
