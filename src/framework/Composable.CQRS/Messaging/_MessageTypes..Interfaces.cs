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

        public interface IMustBeSentTransactionally : MessageTypes.IMessage {}
        public interface IMustBeHandledTransactionally : MessageTypes.IMessage {}
        public interface IMustBeSentAndHandledTransactionally : MessageTypes.IMustBeSentTransactionally, MessageTypes.IMustBeHandledTransactionally {}

        public interface ICannotBeSentRemotelyFromWithinTransaction : MessageTypes.IMessage {}
        public interface IRequireAResponse : MessageTypes.ICannotBeSentRemotelyFromWithinTransaction {}
        public interface IHypermediaMessage : MessageTypes.IRequireAResponse {}
        public interface IHasReturnValue<out TResult> : MessageTypes.IHypermediaMessage {}

        ///<summary>Informs the receiver that something has happened.</summary>
        public interface IEvent : MessageTypes.IMessage {}

        //Enables flexible inheritance and composition
        //Todo: IWrapperEvent name is not great...
        public interface IWrapperEvent<out TEvent> : MessageTypes.IEvent
            where TEvent : IEvent
        {
            TEvent Event { get; }
        }

        public interface ICommand : MessageTypes.IMessage {}
        public interface ICommand<out TResult> : MessageTypes.ICommand, MessageTypes.IHasReturnValue<TResult> {}

        ///<summary>An instructs the receiver to return a result based upon the data in the query.</summary>
        public interface IQuery<out TResult> : MessageTypes.IHasReturnValue<TResult> {}

        ///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the query is sufficient to create the result. For such queries implement this interface. That way no network roundtrip etc is required to perform the query. Greatly enhancing performance</summary>
        public interface ICreateMyOwnResultQuery<out TResult> : MessageTypes.IQuery<TResult>
        {
            TResult CreateResult();
        }

        //Todo: Why do we need both Remotable and Strictly local?
        public static partial class StrictlyLocal
        {
            public interface IMessage {}
            public interface IEvent : MessageTypes.IEvent, StrictlyLocal.IMessage {}
            public interface ICommand : MessageTypes.ICommand, MessageTypes.IMustBeSentTransactionally, StrictlyLocal.IMessage {}
            public interface ICommand<out TResult> : MessageTypes.ICommand<TResult>, StrictlyLocal.ICommand {}
            public interface IQuery<TQuery, out TResult> : MessageTypes.IQuery<TResult>, StrictlyLocal.IMessage where TQuery : IQuery<TQuery, TResult> {}
        }

        //Todo: Why do we need both Remotable and Strictly local?
        public static partial class Remotable
        {
            public interface IMessage : MessageTypes.IMessage {}

            public interface IEvent : Remotable.IMessage, MessageTypes.IEvent {}
            public interface ICommand : MessageTypes.ICommand, Remotable.IMessage {}
            public interface ICommand<out TResult> : Remotable.ICommand, MessageTypes.ICommand<TResult> {}

            ///<summary>A message that is guaranteed not to be delivered more than once. The <see cref="MessageId"/> is used by infrastructure to maintain this guarantee.
            /// The <see cref="MessageId"/> must be maintained when binding a command to a UI or the guarantee will be lost.</summary>
            public interface IAtMostOnceMessage : Remotable.IMessage, MessageTypes.IMustBeHandledTransactionally
            {
                ///<summary>Used by the infrastructure to guarantee that the same message is never delivered more than once. Must be generated when the message is created and then NEVER modified. Must be maintained when binding a command in a UI etc.</summary>
                Guid MessageId { get; }
            }

            //Todo: Why do we need NonTransactional when we have IRequire* interfaces?
            public static partial class NonTransactional
            {
                //todo: Should queries really dictate that handlers may not use transactions to guarantee result consistency if they wish to?
                public interface IQuery<out TResult> : Remotable.IMessage, MessageTypes.IQuery<TResult> {}

                public interface ICreateMyOwnResultQuery<out TResult> : NonTransactional.IQuery<TResult>, MessageTypes.ICreateMyOwnResultQuery<TResult> {}
            }

            //Todo: Is helping with clicking twice in UIs really core logic worth spending time before 1.0 on or should AtMostOnce simply be removed for now?
            public static partial class AtMostOnce
            {
                //todo: dangerous: Validate the design of implementing classes. A default constructor should result in DeduplicationId being Guid.Empty
                public interface IAtMostOnceHypermediaCommand : IAtMostOnceMessage, Remotable.ICommand, MessageTypes.IHypermediaMessage {}
                public interface IAtMostOnceCommand<out TResult> : AtMostOnce.IAtMostOnceHypermediaCommand, Remotable.ICommand<TResult> {}
            }

            public static partial class ExactlyOnce
            {
                //Todo: IRequireTransactionalReceiver seems too restrictive. Surely things such as maintaining in-memory caches, monitoring/debugging tooling etc should be allowed to listen transiently to events without the full exactly once delivery overhead?
                //For commands it makes sense that the message-type dictates such things, but for events it seems like the subscriber should get to choose their preferred way of listening and level of delivery guarantee.
                public interface IMessage : MessageTypes.IMustBeSentAndHandledTransactionally, Remotable.IAtMostOnceMessage {}

                public interface IEvent : Remotable.IEvent, ExactlyOnce.IMessage {}

                //Todo: Should this exist? Or should the wrapped event alone carry this data and metadata? Isn't having it here as well duplication that might cause conflicts with the declaration of the wrapped events?
                //Urgent: Remove for now.
                public interface IWrapperEvent<out TEventInterface> : MessageTypes.IWrapperEvent<TEventInterface>, ExactlyOnce.IEvent
                    where TEventInterface : IEvent
                {
#pragma warning disable CA1033 // Interface methods should be callable by child types: This should be used by infrastructure only. End user code and inheritors should never need to implement it and implementing it in any other way would be a bug.
                    Guid Remotable.IAtMostOnceMessage.MessageId => Event.MessageId;
#pragma warning restore CA1033 // Interface methods should be callable by child types
                }

                public interface ICommand : Remotable.ICommand, ExactlyOnce.IMessage {}
            }
        }
    }
}
