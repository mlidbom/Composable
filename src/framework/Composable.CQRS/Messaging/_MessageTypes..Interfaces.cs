using System;

// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier

namespace Composable.Messaging
{
    public static partial class MessageTypes
    {
        public interface IMessage {}

        public interface IMustBeSentTransactionally : MessageTypes.IMessage {}
        public interface IMustBeHandledTransactionally : MessageTypes.IMessage {}
        public interface IMustBeSentAndHandledTransactionally : MessageTypes.IMustBeSentTransactionally, MessageTypes.IMustBeHandledTransactionally {}

        public interface ICannotBeSentRemotelyFromWithinTransaction : MessageTypes.IMessage {}
        public interface IRequireAResponse : MessageTypes.ICannotBeSentRemotelyFromWithinTransaction {}
        public interface IHypermediaMessage : MessageTypes.IRequireAResponse {}
        public interface IHasReturnValue<out TResult> : MessageTypes.IHypermediaMessage {}

        public interface IEvent : MessageTypes.IMessage {}

        public interface IWrapperEvent<out TEvent> : MessageTypes.IEvent //Todo: IWrapperEvent name is not great...
            where TEvent : IEvent
        {
            TEvent Event { get; }
        }

        public interface ICommand : MessageTypes.IMessage {}
        public interface ICommand<out TResult> : MessageTypes.ICommand, MessageTypes.IHasReturnValue<TResult> {}
    }

    ///<summary>An instructs the receiver to return a result based upon the data in the query.</summary>
    public interface IQuery<out TResult> : MessageTypes.IHasReturnValue<TResult> {}

    ///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the query is sufficient to create the result. For such queries implement this interface. That way no network roundtrip etc is required to perform the query. Greatly enhancing performance</summary>
    public interface ICreateMyOwnResultQuery<out TResult> : IQuery<TResult>
    {
        TResult CreateResult();
    }
    //Todo: Do we need both Remotable and Strictly local?
    public interface IStrictlyLocalMessage {}
    public interface IStrictlyLocalEvent : MessageTypes.IEvent, IStrictlyLocalMessage {}
    public interface IStrictlyLocalCommand : MessageTypes.ICommand, MessageTypes.IMustBeSentTransactionally, IStrictlyLocalMessage {}
    public interface IStrictlyLocalCommand<out TResult> : MessageTypes.ICommand<TResult>, IStrictlyLocalCommand {}
    public interface IStrictlyLocalQuery<TQuery, out TResult> : IQuery<TResult>, IStrictlyLocalMessage where TQuery : IStrictlyLocalQuery<TQuery, TResult> {}
    //Todo: Why do we need both Remotable and Strictly local?
    public interface IRemotableMessage : MessageTypes.IMessage {}
    public interface IRemotableEvent : IRemotableMessage, MessageTypes.IEvent {}
    public interface IRemotableCommand : MessageTypes.ICommand, IRemotableMessage {}
    public interface IRemotableCommand<out TResult> : IRemotableCommand, MessageTypes.ICommand<TResult> {}
    public interface IRemotableQuery<out TResult> : IRemotableMessage, IQuery<TResult> {}
    public interface IRemotableCreateMyOwnResultQuery<out TResult> : IRemotableQuery<TResult>, ICreateMyOwnResultQuery<TResult> {}

    //Todo: Is helping with clicking twice in UIs really core logic worth spending time before 1.0 on or should AtMostOnce simply be removed for now?
    ///<summary>A message that is guaranteed not to be delivered more than once. The <see cref="MessageId"/> is used by infrastructure to maintain this guarantee.
    /// The <see cref="MessageId"/> must be maintained when binding a command to a UI or the guarantee will be lost.</summary>
    public interface IAtMostOnceMessage : IRemotableMessage, MessageTypes.IMustBeHandledTransactionally
    {
        ///<summary>Used by the infrastructure to guarantee that the same message is never delivered more than once. Must be generated when the message is created and then NEVER modified. Must be maintained when binding a command in a UI etc.</summary>
        Guid MessageId { get; }
    }
    public interface IAtMostOnceHypermediaCommand : IAtMostOnceMessage, IRemotableCommand, MessageTypes.IHypermediaMessage {}
    public interface IAtMostOnceCommand<out TResult> : IAtMostOnceHypermediaCommand, IRemotableCommand<TResult> {}
    public interface IExactlyOnceMessage : MessageTypes.IMustBeSentAndHandledTransactionally, IAtMostOnceMessage {}

    //Todo: IRequireTransactionalReceiver seems too restrictive. Surely things such as maintaining in-memory caches, monitoring/debugging tooling etc should be allowed to listen transiently to events without the full exactly once delivery overhead?
    //For commands it makes sense that the message-type dictates such things, but for events it seems like the subscriber should get to choose their preferred way of listening and level of delivery guarantee.
    public interface IExactlyOnceEvent : IRemotableEvent, IExactlyOnceMessage {}
    public interface IExactlyOnceCommand : IRemotableCommand, IExactlyOnceMessage {}

    //Todo: Should this exist? Or should the wrapped event alone carry this data and metadata? Isn't having it here as well duplication that might cause conflicts with the declaration of the wrapped events?
    //Urgent: Remove for now.
    public interface IExactlyOnceWrapperEvent<out TEventInterface> : MessageTypes.IWrapperEvent<TEventInterface>, IExactlyOnceEvent
        where TEventInterface : IExactlyOnceEvent
    {
#pragma warning disable CA1033 // Interface methods should be callable by child types: This should be used by infrastructure only. End user code and inheritors should never need to implement it and implementing it in any other way would be a bug.
        Guid IAtMostOnceMessage.MessageId => Event.MessageId;
#pragma warning restore CA1033 // Interface methods should be callable by child types
    }
}
