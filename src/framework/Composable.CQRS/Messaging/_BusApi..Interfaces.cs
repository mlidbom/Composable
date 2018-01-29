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

        ///<summary>An instructs the receiver to return a resource based upon the data in the query.</summary>
        public interface IQuery<TResult> : BusApi.IQuery { }

        public interface ICreateMyOwnResultQuery<TResult> : IQuery<TResult>
        {
            TResult CreateResult();
        }

        public static partial class StrictlyLocal
        {
            public interface IRequireLocalReceiver {}
            public interface IEvent : BusApi.IEvent, IRequireLocalReceiver { }
            public interface ICommand : BusApi.ICommand, IRequireLocalReceiver, IRequireTransactionalSender { }
            public interface ICommand<TResult> : BusApi.ICommand<TResult>, IRequireLocalReceiver  { }
            public interface IQuery<TResult> : IRequireLocalReceiver, BusApi.IQuery<TResult> { }
        }

        public static partial class RemoteSupport
        {
            public interface IMessage : BusApi.IMessage {}
            public interface IRequireRemoteResponse : RemoteSupport.IMessage {}
            public interface ICommand : BusApi.ICommand, RemoteSupport.IMessage { }
            public interface ICommand<TResult> : ICommand, BusApi.ICommand<TResult>, IRequireRemoteResponse { }

            public static partial class NonTransactional
            {
                public interface IMessage : IForbidTransactionalRemoteSender, RemoteSupport.IMessage {}
                public interface IQuery : RemoteSupport.IRequireRemoteResponse, NonTransactional.IMessage, BusApi.IQuery { }
                public interface IQuery<TResult> : NonTransactional.IQuery, BusApi.IQuery<TResult> { }
            }

            public static partial class AtMostOnce
            {
                public interface ICommand : RemoteSupport.ICommand, IRequireRemoteResponse, IForbidTransactionalRemoteSender { }
                public interface ICommand<TResult> : AtMostOnce.ICommand, RemoteSupport.ICommand<TResult> { }
            }

            public static partial class ExactlyOnce
            {
                public interface IMessage : IRequireAllOperationsToBeTransactional, IProvidesOwnMessageId, BusApi.RemoteSupport.IMessage {}
                public interface IRequireAllOperationsToBeTransactional : IRequireTransactionalSender, IRequireTransactionalReceiver {}

                public interface IProvidesOwnMessageId { Guid MessageId { get; } }

                public interface IEvent : BusApi.IEvent, IMessage { }
                public interface ICommand : RemoteSupport.ICommand, IMessage { }
                public interface ICommand<TResult> : RemoteSupport.ICommand<TResult>, ExactlyOnce.ICommand { }
            }
        }
    }
}
