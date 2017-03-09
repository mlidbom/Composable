using System;
using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi;

namespace Composable.HyperBus.DemoApp.ApiImplementation.MessageHandlers
{
    public class AccountEmailer :
        IEventHandler<AccountEvent.IAccountRegisteredEvent>,
        IEventHandler<AccountEvent.IAccountEmailChangedEvent>
    {
        public void Handle(AccountEvent.IAccountRegisteredEvent @event) { throw new NotImplementedException(); }
        public void Handle(AccountEvent.IAccountEmailChangedEvent @event) { throw new NotImplementedException(); }
    }
}