using System;

// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier

namespace Composable.Messaging
{
    public static partial class MessageTypes
    {
        ///<summary>Any object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
        public interface IMessage {}

        public interface IRequireTransactionalSender : MessageTypes.IMessage {}
        public interface IRequireTransactionalReceiver : MessageTypes.IMessage {}
        public interface IRequireAllOperationsToBeTransactional : MessageTypes.IRequireTransactionalSender, MessageTypes.IRequireTransactionalReceiver {}

        public interface IRequireResponse : MessageTypes.IMessage {}
        public interface ICannotBeSentRemotelyFromWithinTransaction : MessageTypes.IMessage {}
        public interface IHasReturnValue<out TResult> : MessageTypes.IRequireResponse, ICannotBeSentRemotelyFromWithinTransaction {}

        ///<summary>Informs the receiver that something has happened.</summary>
        public interface IEvent : MessageTypes.IMessage {}

        //Enables flexible inheritance and composition
        //Todo: IWrapperEvent name is not great...
        public interface IWrapperEvent<out TEvent> : MessageTypes.IEvent
            where TEvent : IEvent
        {
            TEvent Event { get; }
        }

        //Todo: Should there really be no commands that may have non-transactional receivers?
        public interface ICommand : MessageTypes.IRequireTransactionalReceiver {}
        public interface ICommand<out TResult> : MessageTypes.ICommand, MessageTypes.IHasReturnValue<TResult> {}

        ///<summary>An instructs the receiver to return a result based upon the data in the query.</summary>
        public interface IQuery<out TResult> : MessageTypes.IMessage {}

        ///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the query is sufficient to create the result. For such queries implement this interface. That way no network roundtrip etc is required to perform the query. Greatly enhancing performance</summary>
        public interface ICreateMyOwnResultQuery<out TResult> : MessageTypes.IQuery<TResult>
        {
            TResult CreateResult();
        }

        public static partial class StrictlyLocal
        {
            public interface IMessage {}
            public interface IEvent : MessageTypes.IEvent, StrictlyLocal.IMessage {}
            public interface ICommand : MessageTypes.ICommand, MessageTypes.IRequireTransactionalSender, StrictlyLocal.IMessage {}
            public interface ICommand<out TResult> : MessageTypes.ICommand<TResult>, StrictlyLocal.ICommand {}
            public interface IQuery<TQuery, out TResult> : MessageTypes.IQuery<TResult>, StrictlyLocal.IMessage where TQuery : IQuery<TQuery, TResult> {}
        }

        public static partial class Remotable
        {
            public interface IMessage : MessageTypes.IMessage {}

            public interface IEvent : Remotable.IMessage, MessageTypes.IEvent {}
            public interface ICommand : MessageTypes.ICommand, Remotable.IMessage {}
            public interface ICommand<out TResult> : ICommand, MessageTypes.ICommand<TResult>, MessageTypes.IHasReturnValue<TResult> {}

            public static partial class NonTransactional
            {
                public interface IMessage : ICannotBeSentRemotelyFromWithinTransaction, Remotable.IMessage {}
                public interface IQuery<out TResult> : NonTransactional.IMessage, MessageTypes.IQuery<TResult>, MessageTypes.IHasReturnValue<TResult> {}

                public interface ICreateMyOwnResultQuery<out TResult> : IQuery<TResult>, MessageTypes.ICreateMyOwnResultQuery<TResult> {}
            }

            public static partial class AtMostOnce
            {
                ///<summary>A message that is guaranteed not to be delivered more than once. The <see cref="MessageId"/> is used by infrastructure to maintain this guarantee.
                /// The <see cref="MessageId"/> must be maintained when binding a command to a UI or the guarantee will be lost.</summary>
                public interface IMessage : Remotable.IMessage
                {
                    ///<summary>Used by the infrastructure to guarantee that the same message is never delivered more than once. Must be generated when the message is created and then NEVER modified. Must be maintained when binding a command in a UI etc.</summary>
                    Guid MessageId { get; }
                }

                //todo: dangerous: Validate the design of implementing classes. A default constructor should result in DeduplicationId being Guid.Empty
                //Urgent: IRequireResponse here indicates something about this command. Namely that it is a Hypermedia Command I think. Not a fire-and-forget command
                public interface ICommand : IMessage, Remotable.ICommand, MessageTypes.IRequireResponse {}
                public interface ICommand<out TResult> : AtMostOnce.ICommand, Remotable.ICommand<TResult> {}
            }

            public static partial class ExactlyOnce
            {
                //Todo: IRequireTransactionalReceiver seems too restrictive. Surely things such as maintaining in-memory caches, monitoring/debugging tooling etc should be allowed to listen transiently to events without the full exactly once delivery overhead?
                public interface IMessage : MessageTypes.IRequireAllOperationsToBeTransactional, AtMostOnce.IMessage {}

                public interface IEvent : Remotable.IEvent, ExactlyOnce.IMessage {}

                public interface IWrapperEvent<out TEventInterface> : MessageTypes.IWrapperEvent<TEventInterface>, ExactlyOnce.IEvent
                    where TEventInterface : IEvent
                {
#pragma warning disable CA1033 // Interface methods should be callable by child types: This should be used by infrastructure only. End user code and inheritors should never need to implement it and implementing it in any other way would be a bug.
                    Guid AtMostOnce.IMessage.MessageId => Event.MessageId;
#pragma warning restore CA1033 // Interface methods should be callable by child types
                }

                public interface ICommand : Remotable.ICommand, ExactlyOnce.IMessage {}
            }
        }
    }
}
