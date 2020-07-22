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

        public interface IForbidTransactionalRemoteSender : IMessage { }
        public interface IRequireTransactionalReceiver : IMessage { }
        public interface IRequireTransactionalSender : IMessage{ }

        ///<summary>Informs the receiver that something has happened.</summary>
        public interface IEvent : IMessage { }

        public interface IHasReturnValue : IMessage
        {}

        /// <summary>Instructs the receiver to perform an action.</summary>
        public interface ICommand : IRequireTransactionalReceiver { }
        public interface ICommand<TResult> : ICommand, IHasReturnValue{ }

        public interface IQuery : IForbidTransactionalRemoteSender, IHasReturnValue { }

        ///<summary>An instructs the receiver to return a result based upon the data in the query.</summary>
        public interface IQuery<TResult> : MessageTypes.IQuery { }

        ///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the query is sufficient to create the result. For such queries implement this interface. That way no network roundtrip etc is required to perform the query. Greatly enhancing performance</summary>
        public interface ICreateMyOwnResultQuery<TResult> : IQuery<TResult>
        {
            TResult CreateResult();
        }

        public static partial class StrictlyLocal
        {
            public interface IMessage {}
            public interface IEvent : MessageTypes.IEvent, StrictlyLocal.IMessage { }
            public interface ICommand : MessageTypes.ICommand, IRequireTransactionalSender, StrictlyLocal.IMessage { }
            public interface ICommand<TResult> : MessageTypes.ICommand<TResult>, IRequireTransactionalSender, StrictlyLocal.ICommand, StrictlyLocal.IMessage  { }
            public interface IQuery<TQuery, TResult> : MessageTypes.IQuery<TResult>, StrictlyLocal.IMessage
            where TQuery : IQuery<TQuery, TResult>
            { }
        }

        public static partial class Remotable
        {
            ///<summary>A message that is guaranteed not to be delivered more than once. The <see cref="MessageId"/> is used by infrastructure to maintain this guarantee.
            /// The <see cref="MessageId"/> must be maintained when binding a command to a UI or the guarantee will be lost.</summary>
            public interface IAtMostOnceMessage
            {
                ///<summary>Used by the infrastructure to guarantee that the same message is never delivered more than once. Must be generated when the message is created and then NEVER modified. Must be maintained when binding a command in a UI etc.</summary>
                Guid MessageId { get; }
            }

            public interface IMessage : MessageTypes.IMessage {}
            public interface IEvent : Remotable.IMessage, MessageTypes.IEvent {}
            public interface IRequireRemoteResponse : Remotable.IMessage {}
            public interface ICommand : MessageTypes.ICommand, Remotable.IMessage { }
            public interface ICommand<TResult> : ICommand, MessageTypes.ICommand<TResult>, IRequireRemoteResponse { }

            public static partial class NonTransactional
            {
                public interface IMessage : IForbidTransactionalRemoteSender, Remotable.IMessage {}
                public interface IQuery : Remotable.IRequireRemoteResponse, NonTransactional.IMessage, MessageTypes.IQuery { }
                public interface IQuery<TResult> : NonTransactional.IQuery, MessageTypes.IQuery<TResult> { }
            }

            public static partial class AtMostOnce
            {
                //todo: dangerous: Validate the design of implementing classes. A default constructor should result in DeduplicationId being Guid.Empty
                public interface ICommand : IAtMostOnceMessage, Remotable.ICommand, IRequireRemoteResponse, IForbidTransactionalRemoteSender { }
                public interface ICommand<TResult> : AtMostOnce.ICommand, Remotable.ICommand<TResult> { }
            }

            public static partial class ExactlyOnce
            {
                public interface IMessage : IRequireAllOperationsToBeTransactional, IAtMostOnceMessage, Remotable.IMessage {}
                public interface IRequireAllOperationsToBeTransactional : IRequireTransactionalSender, IRequireTransactionalReceiver {}

                public interface IEvent : Remotable.IEvent, ExactlyOnce.IMessage { }
                public interface ICommand : Remotable.ICommand, ExactlyOnce.IMessage { }
            }
        }
    }
}
