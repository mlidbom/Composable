using System;
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass
// ReSharper disable RedundantNameQualifier

namespace Composable.Messaging
{
    public static partial class BusApi
    {
        public interface IForbidTransactionalRemoteDispatching { }

        ///<summary>Any object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
        public interface IMessage {}

        ///<summary>Informs the receiver that something has happened.</summary>
        public interface IEvent : IMessage { }

        /// <summary>Instructs the recevier to perform an action.</summary>
        public interface ICommand : IMessage { }
        public interface ICommand<TResult> : ICommand{ }

        public interface IQuery : IMessage, IForbidTransactionalRemoteDispatching { }

        ///<summary>An instructs the receiver to return a resource based upon the data in the query.</summary>
        public interface IQuery<TResult> : BusApi.IQuery { }

        public interface ICreateMyOwnResultQuery<TResult> : IQuery<TResult>
        {
            TResult CreateResult();
        }

        public static partial class Local
        {
            public interface IRequireLocalReceiver {}
            public interface IEvent : BusApi.IEvent, IRequireLocalReceiver { }
            public interface ICommand : BusApi.ICommand, IRequireLocalReceiver { }
            public interface ICommand<TResult> : BusApi.ICommand<TResult>, IRequireLocalReceiver  { }
            public interface IQuery<TResult> : IRequireLocalReceiver, BusApi.IQuery<TResult> { }
        }

        public static partial class Remote
        {
            public interface ISupportRemoteReceiverMessage : IMessage {}
            public interface IRequireRemoteResponse : ISupportRemoteReceiverMessage {}
            public interface ICommand : BusApi.ICommand, ISupportRemoteReceiverMessage { }
            public interface ICommand<TResult> : ICommand, BusApi.ICommand<TResult>, IRequireRemoteResponse { }

            public static partial class NonTransactional
            {
                public interface IMessage : IForbidTransactionalRemoteDispatching, ISupportRemoteReceiverMessage {}
                public interface IQuery : Remote.IRequireRemoteResponse, Remote.NonTransactional.IMessage, BusApi.IQuery { }
                public interface IQuery<TResult> : Remote.NonTransactional.IQuery, BusApi.IQuery<TResult> { }
            }

            public static partial class AtMostOnce
            {
                public interface ICommand : BusApi.Remote.ICommand, IRequireRemoteResponse { }
                public interface ICommand<TResult> : BusApi.Remote.AtMostOnce.ICommand, BusApi.Remote.ICommand<TResult> { }
            }

            public static partial class ExactlyOnce
            {
                public interface IRequireTransactionalSender : ISupportRemoteReceiverMessage{ }
                public interface IRequireTransactionalReceiver : ISupportRemoteReceiverMessage { }
                public interface IRequireAllOperationsToBeTransactional : IRequireTransactionalSender, IRequireTransactionalReceiver {}

                public interface IProvidesOwnMessageId { Guid MessageId { get; } }
                public interface IExactlyOnceMessage : IRequireAllOperationsToBeTransactional, IProvidesOwnMessageId {}

                public interface IEvent : BusApi.IEvent, IExactlyOnceMessage { }
                public interface ICommand : BusApi.Remote.ICommand, IExactlyOnceMessage { }
                public interface ICommand<TResult> : BusApi.Remote.ICommand<TResult>, BusApi.Remote.ExactlyOnce.ICommand { }
            }
        }
    }
}
