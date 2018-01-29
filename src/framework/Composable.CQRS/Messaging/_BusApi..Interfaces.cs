using System;
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public partial class BusApi
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
        public interface IQuery<TResult> : BusApi.IQuery { }

        public interface ICreateMyOwnResultQuery<TResult> : IQuery<TResult>
        {
            TResult CreateResult();
        }

        public partial class Local
        {
            public interface IRequireLocalReceiver {}
            public interface IEvent : BusApi.IEvent, IRequireLocalReceiver { }
            public interface ICommand : BusApi.ICommand, IRequireLocalReceiver { }
            public interface ICommand<TResult> : BusApi.ICommand<TResult>, IRequireLocalReceiver  { }
            public interface IQuery<TResult> : IRequireLocalReceiver, BusApi.IQuery<TResult> { }
        }

        public partial class Remote
        {
            public interface ISupportRemoteReceiverMessage : IMessage {}

            public partial class NonTransactional
            {
                public interface IAtMostOnceDelivery {}
                public interface IForbidTransactionalSend { }

                public interface IMessage : IForbidTransactionalSend, IAtMostOnceDelivery, ISupportRemoteReceiverMessage {}
                public interface IQuery : BusApi.IQuery, IMessage { }
                public interface IQuery<TResult> : BusApi.IQuery<TResult>, BusApi.Remote.NonTransactional.IQuery, IMessage { }
            }

            public partial class AtMostOnce
            {
                public interface ICommand : BusApi.ICommand, ISupportRemoteReceiverMessage { }
                public interface ICommand<TResult> : ICommand, BusApi.ICommand<TResult> { }
            }

            public partial class ExactlyOnce
            {
                public interface IRequireTransactionalSender : ISupportRemoteReceiverMessage{ }
                public interface IRequireTransactionalReceiver : ISupportRemoteReceiverMessage { }
                public interface IRequireAllOperationsToBeTransactional : IRequireTransactionalSender, IRequireTransactionalReceiver {}

                public interface IProvidesOwnMessageId { Guid MessageId { get; } }
                public interface IExactlyOnceMessage : IRequireAllOperationsToBeTransactional, IProvidesOwnMessageId {}

                public interface IEvent : BusApi.IEvent, IExactlyOnceMessage { }
                public interface ICommand : BusApi.ICommand, IExactlyOnceMessage { }
                public interface ICommand<TResult> : BusApi.ICommand<TResult>, BusApi.Remote.ExactlyOnce.ICommand { }
            }
        }
    }
}
