using System;
// ReSharper disable UnusedTypeParameter
// ReSharper disable MemberHidesStaticFromOuterClass

namespace Composable.Messaging
{
    public partial class MessagingApi
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
        public interface IQuery<TResult> : MessagingApi.IQuery { }

        public partial class Local
        {
            public interface IRequireLocalReceiver {}
            public interface IEvent : MessagingApi.IEvent, IRequireLocalReceiver { }
            public interface ICommand : MessagingApi.ICommand, IRequireLocalReceiver { }
            public interface ICommand<TResult> : MessagingApi.ICommand<TResult>, IRequireLocalReceiver  { }
            public interface IQuery<TResult> : IRequireLocalReceiver, MessagingApi.IQuery<TResult> { }
        }

        public partial class Remote
        {
            public interface ISupportRemoteReceiver {}

            public partial class NonTransactional
            {
                public interface IAtMostOnceDelivery {}
                public interface IForbidTransactionalSend { }

                public interface IMessage : IForbidTransactionalSend, IAtMostOnceDelivery, ISupportRemoteReceiver {}
                public interface ICommand<TResult> : MessagingApi.ICommand<TResult>, IMessage  { }
                public interface IQuery : MessagingApi.IQuery, IMessage { }
                public interface IQuery<TResult> : MessagingApi.IQuery<TResult>, MessagingApi.Remote.NonTransactional.IQuery, IMessage { }
            }

            public partial class ExactlyOnce
            {
                public interface IRequireTransactionalSender : ISupportRemoteReceiver{ }
                public interface IRequireTransactionalReceiver : ISupportRemoteReceiver { }
                public interface IRequireAllOperationsToBeTransactional : IRequireTransactionalSender, IRequireTransactionalReceiver {}

                public interface IProvidesOwnMessageId { Guid MessageId { get; } }
                public interface IExactlyOnceMessage : IRequireAllOperationsToBeTransactional, IProvidesOwnMessageId {}

                public interface IEvent : MessagingApi.IEvent, IExactlyOnceMessage { }
                public interface ICommand : MessagingApi.ICommand, IExactlyOnceMessage { }
                public interface ICommand<TResult> : MessagingApi.ICommand<TResult>, MessagingApi.Remote.ExactlyOnce.ICommand { }
            }
        }
    }
}
