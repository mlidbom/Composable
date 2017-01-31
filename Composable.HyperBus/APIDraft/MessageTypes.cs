using System;
// ReSharper disable UnusedMember.Global

namespace Composable.HyperBus.APIDraft
{
    public interface IMessage { }

    public interface IEvent : IMessage { }

    public interface ICommand : IMessage { }

    public interface ICommand<TReturnValue> : ICommand { }

    public interface IQuery<TReturnValue> : IMessage { }

    public class Command : ICommand { public Guid Id { get; } }

    public class Command<TReturnValue> : Command, ICommand<TReturnValue> { }
}