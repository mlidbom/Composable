using Composable.HyperBus.APIDraft;

namespace Composable.HyperBus.DemoApp.ExposedApi
{
    public static class AccountEvent
    {
        public interface IAccountRegisteredEvent : IEvent { }
        public interface IAccountEmailChangedEvent : IEvent { }
    }
}