using System;
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier

namespace Composable.Messaging
{
    public static partial class BusApi
    {
        ///<summary>Any object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
        public interface IMessage {}

        public interface IForbidTransactionalRemoteSender : IMessage { }
        public interface IRequireTransactionalReceiver : IMessage { }
        public interface IRequireTransactionalSender : IMessage{ }

        ///<summary>Informs the receiver that something has happened.</summary>
        public interface IEvent : IMessage { }

        /// <summary>Instructs the recevier to perform an action.</summary>
        public interface ICommand : IRequireTransactionalReceiver { }
        public interface ICommand<TResult> : ICommand{ }

        public interface IQuery : IForbidTransactionalRemoteSender { }

        ///<summary>An instructs the receiver to return a result based upon the data in the query.</summary>
        public interface IQuery<TResult> : BusApi.IQuery { }

        ///<summary>Many resources in a hypermedia API do not actually need access to backend data. The data in the query is sufficient to create the result. For such queries implement this interface. That way no network roundtrip etc is required to perform the query. Greatly enhancing performance</summary>
        public interface ICreateMyOwnResultQuery<TResult> : IQuery<TResult>
        {
            TResult CreateResult();
        }

        public static partial class StrictlyLocal
        {
            public interface IMessage {}
            public interface IEvent : BusApi.IEvent, StrictlyLocal.IMessage { }
            public interface ICommand : BusApi.ICommand, IRequireTransactionalSender, StrictlyLocal.IMessage { }
            public interface ICommand<TResult> : BusApi.ICommand<TResult>, IRequireTransactionalSender, StrictlyLocal.ICommand, StrictlyLocal.IMessage  { }
            public interface IQuery<TResult> : BusApi.IQuery<TResult>, StrictlyLocal.IMessage { }
        }

        public static partial class Remotable
        {
            ///<summary>A message that is guaranteed not to be delivered more than once. The <see cref="DeduplicationId"/> is used by infrastructure to maintain this guarantee.
            /// The <see cref="DeduplicationId"/> must be maintained when binding a command to a UI or the guarantee will be lost.</summary>
            public interface IAtMostOnceMessage
            {
                ///<summary>Used by the infrastructure to guarantee that the same message is never delivered more than once. Must be generated when the message is created and then NEVER modified. Must be maintained when binding a command in a UI etc.</summary>
                Guid DeduplicationId { get; }
            }

            public interface IMessage : BusApi.IMessage {}
            public interface IEvent : Remotable.IMessage, BusApi.IEvent {}
            public interface IRequireRemoteResponse : Remotable.IMessage {}
            public interface ICommand : BusApi.ICommand, Remotable.IMessage { }
            public interface ICommand<TResult> : ICommand, BusApi.ICommand<TResult>, IRequireRemoteResponse { }

            public static partial class NonTransactional
            {
                public interface IMessage : IForbidTransactionalRemoteSender, Remotable.IMessage {}
                public interface IQuery : Remotable.IRequireRemoteResponse, NonTransactional.IMessage, BusApi.IQuery { }
                public interface IQuery<TResult> : NonTransactional.IQuery, BusApi.IQuery<TResult> { }
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
