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

    public interface IProvidesOwnMessageId { Guid MessageId { get; } }

    public interface ISupportRemoteDelivery {}

    public interface IOnlyLocalDelivery {}
    public interface ILocalEvent : IEvent, IOnlyLocalDelivery { }
    public interface ILocalCommand : ICommand, IOnlyLocalDelivery { }
    public interface ILocalCommand<TResult> : ICommand<TResult>, IOnlyLocalDelivery  { }
    public interface ILocalQuery<TResult> : IOnlyLocalDelivery, IQuery<TResult> { }


    public interface IAtMostOnceDelivery {}
    public interface IForbidTransactionalSendOperation { }

    public interface IUserInterfaceMessage : IForbidTransactionalSendOperation, IAtMostOnceDelivery, ISupportRemoteDelivery {}
    public interface IUserInterfaceCommand<TResult> : ICommand<TResult>, IUserInterfaceMessage  { }
    public interface IUserInterfaceQuery<TResult> : IQuery<TResult>, IUserInterfaceMessage { }

    public interface IRequireTransactionalSendOperation : ISupportRemoteDelivery{ }
    public interface IRequireTransactionalHandlerExecution : ISupportRemoteDelivery { }
    public interface IRequireAllOperationsToBeTransactional : IRequireTransactionalSendOperation, IRequireTransactionalHandlerExecution {}

    public interface IExactlyOnceMessage : IRequireAllOperationsToBeTransactional, IProvidesOwnMessageId {}

    public interface IExactlyOnceEvent : IEvent, IExactlyOnceMessage { }
    public interface IExactlyOnceCommand : ICommand, IExactlyOnceMessage { }
    public interface IExactlyOnceCommand<TResult> : ICommand<TResult>, IExactlyOnceCommand { }
}
