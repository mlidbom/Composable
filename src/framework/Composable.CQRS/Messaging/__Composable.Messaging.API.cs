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

    public interface IRemoteMessage : IMessage {}

    public interface ILocalMessage : IMessage {}
    public interface ILocalEvent : IEvent, ILocalMessage { }
    public interface ILocalCommand : ICommand, ILocalMessage { }
    public interface ILocalCommand<TResult> : ICommand<TResult>, ILocalMessage  { }
    public interface ILocalQuery<TResult> : ILocalMessage, IQuery<TResult> { }


    public interface IForbidTransactionalSendOperationMessage : IRemoteMessage{ }

    public interface IUserInterfaceMessage : IForbidTransactionalSendOperationMessage, IMessage {}
    public interface IUserInterfaceCommand<TResult> : ICommand<TResult>, IUserInterfaceMessage  { }
    public interface IUserInterfaceQuery<TResult> : IUserInterfaceMessage, IQuery<TResult> { }

    public interface IRequiresTransactionalSendOperationMessage : IRemoteMessage{ }
    public interface IRequireTransactionalHandlerExecutionMessage : IRemoteMessage { }
    public interface IRequireAllOperationsToBeTransactionalMessage : IRequiresTransactionalSendOperationMessage, IRequireTransactionalHandlerExecutionMessage {}

    public interface IAtMostOnceMessage : IRequireAllOperationsToBeTransactionalMessage {}
    public interface IExactlyOnceMessage : IRequireAllOperationsToBeTransactionalMessage, IProvidesOwnMessageIdMessage {}

    public interface IAtMostOnceEvent : IEvent, IAtMostOnceMessage { }
    public interface IAtMostOnceCommand : ICommand, IAtMostOnceMessage { }
    public interface IAtMostOnceCommand<TResult> : ICommand<TResult>, IAtMostOnceMessage  { }

    public interface IExactlyOnceEvent : IEvent, IExactlyOnceMessage { }
    public interface IExactlyOnceCommand : ICommand, IExactlyOnceMessage { }
    public interface IExactlyOnceCommand<TResult> : ICommand<TResult>, IExactlyOnceCommand { }
}
