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
            public interface IMessage : BusApi.IMessage {}
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
                public interface ICommand : Remotable.ICommand, IRequireRemoteResponse, IForbidTransactionalRemoteSender { }
                public interface ICommand<TResult> : AtMostOnce.ICommand, Remotable.ICommand<TResult> { }
            }

            public static partial class ExactlyOnce
            {
                public interface IMessage : IRequireAllOperationsToBeTransactional, IProvidesOwnMessageId, BusApi.Remotable.IMessage {}
                public interface IRequireAllOperationsToBeTransactional : IRequireTransactionalSender, IRequireTransactionalReceiver {}

                public interface IProvidesOwnMessageId { Guid MessageId { get; } }

                public interface IEvent : BusApi.IEvent, IMessage { }
                public interface ICommand : Remotable.ICommand, IMessage { }
            }
        }
    }
}
