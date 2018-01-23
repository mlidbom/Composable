using System;

// ReSharper disable UnusedTypeParameter

namespace Composable.Messaging
{
    ///<summary>Any object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
    public interface IMessage {}

    ///<summary>Informs the receiver that something has happened.</summary>
    public interface IEvent : IMessage { }

    /// <summary>Instructs the recevier to perform an action.</summary>
    public interface ICommand : IMessage { }
    public interface ICommand<TResult> : ICommand { }

    public interface IQuery : IMessage { }

    ///<summary>An instructs the receiver to return a resource based upon the data in the query.</summary>
    public interface IQuery<TResult> : IQuery { }

    public interface IProvidesOwnMessageIdMessage : IMessage { Guid MessageId { get; } }

    public interface IParticipateInTransactionalSendOperationMessage : IMessage { }
    public interface IRequiresTransactionalSendOperationMessage : IParticipateInTransactionalSendOperationMessage { }
    public interface IRequireTransactionalHandlerExecutionMessage : IMessage { }
    public interface IRequireAllOperationsToBeTransactionalMessage : IRequiresTransactionalSendOperationMessage, IRequireTransactionalHandlerExecutionMessage {}

    public interface IAtMostOnceMessage : IRequireAllOperationsToBeTransactionalMessage {}
    public interface IAtLeastOnceMessage : IRequireAllOperationsToBeTransactionalMessage, IProvidesOwnMessageIdMessage { }
    public interface IExactlyOnceMessage : IAtLeastOnceMessage { }

    public interface IAtMostOnceEvent : IEvent, IAtMostOnceMessage { }
    public interface IAtMostOnceCommand : ICommand, IAtMostOnceMessage { }
    public interface IAtMostOnceCommand<TResult> : ICommand<TResult>, IAtMostOnceMessage  { }

    public interface IExactlyOnceEvent : IEvent, IExactlyOnceMessage { }
    public interface IExactlyOnceCommand : ICommand, IExactlyOnceMessage { }
    public interface IExactlyOnceCommand<TResult> : ICommand<TResult>, IExactlyOnceCommand { }
}
