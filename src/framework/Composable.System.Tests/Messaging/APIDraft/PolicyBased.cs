// ReSharper disable All

namespace Composable.Tests.Messaging.APIDraft
{
    public class PolicyBased
    {
        interface IThreadingPolicy { } //IEnumerable<string> LocksToTake(IMessage message);

        interface ITransactionPolicy { } // String TransactionToParticipateIn(IMessage message)

        class OneOperationOnAnAggregateAtATime: IThreadingPolicy { }
        class OneHandlerAtATimePerMessage : IThreadingPolicy {}
        class OneMessageAtATime : IThreadingPolicy { }
        class MultipleMessagesAtATime : IThreadingPolicy { }


        class TransactionPerMessage : ITransactionPolicy { }
        class TransactionPerHandler : ITransactionPolicy { }


        class MessageHandler
        {
            public MessageHandler(params object[] configurations){}
        }

        class HandlerGroup
        {
            public HandlerGroup(params object[] parameters) {}
        }

        class Endpoint
        {
            public Endpoint(params object[] messageHandlers) { }
        }


        enum MessageThreadingPolicy { Serialized, Parallel, SerializeAggregateAccess }
        enum HandlerInvokation { InRegistrationOrder, InParallel }
        enum TransactionBoundary { Message, Handler }
        enum HandlerFailurePolicy { ContinueWithOtherHandlers, StopInvokingHandlers }

        //When sending commands the sender should specify if they wish in-transaction execution so no equivalent option for commands is needed.
        //Maybe the Synchronous option here should be a completely different type of  EventHandler / EventHandler-registration?
        enum EndpointInternalEventCascadePolicy { Synchronous, Asynchronous }

        void IllustratateRegistration()
        {
            var policiesAsInterfaces = new Endpoint(

                new MessageHandler(
                    new OneHandlerAtATimePerMessage(),//Only one handler at a time can handle a specific message.
                    new OneOperationOnAnAggregateAtATime())//Only one handler at a time can handle a message about a certain aggregate.
                );

            var policiesAsEnums =
                new Endpoint("ADomainEndpoint",
                             new HandlerGroup(
                                 "Command handlers",
                                 MessageThreadingPolicy.Parallel, //Commands should be handled in parallel or we essentially single thread our entire endpoint/service.
                                 MessageThreadingPolicy.SerializeAggregateAccess, //It is useless to try to execute more than one modification of the same aggregate at a time, so let's not waste resources trying.


                                 HandlerInvokation.InRegistrationOrder, //Meaningless since there can only be one command handler.
                                 TransactionBoundary.Message, //Meaningless since there can only be one command handler.
                                 EndpointInternalEventCascadePolicy.Asynchronous, //Invalid for commands. Overriding the default async behavior should be done by the caller of send, Be part of the bus API

                                 new MessageHandler("command handler 1"),
                                 new MessageHandler("command handler 2"),
                                 new MessageHandler("command handler 3"),
                                 new MessageHandler("command handler 4")),
                             new HandlerGroup(
                                 "Query model updaters",
                                 EndpointInternalEventCascadePolicy.Synchronous, //Domain query models should be immediatelly consistent if at all possible..
                                 TransactionBoundary.Message, //Setting anything else together with EndpointInternalEventCascadePolicy.Synchronous would be illegal.

                                 new MessageHandler("Account email query model updater")
                             ),
                             new HandlerGroup(
                                 HandlerInvokation.InParallel,
                                 new MessageHandler(MessageThreadingPolicy.Parallel, "Slow event handler that often receives batches of events from one aggregate. We have verified that handling messages in parallel is safe and it is necessary for latency reasons.")
                                 )
                );
        }
    }
}
