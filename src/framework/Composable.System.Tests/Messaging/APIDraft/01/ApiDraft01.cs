using System;
using Composable.System.Linq;

// ReSharper disable All

namespace Composable.Tests.Messaging.APIDraft._01
{
    class APIDraft01
    {
        class MessageHandler<TImplementation>
        {
            public MessageHandler<TImplementation> ForEvent<TEvent>(Action<TImplementation, TEvent> action) => this;
            public MessageHandler<TImplementation> ForCommand<TCommand>(Action<TImplementation, TCommand> action) => this;
            public MessageHandler<TImplementation> ForQuery<TQuery, TResult>(Func<TImplementation, TQuery, TResult> action) => this;


        }

        class MessageHandler
        {
            public MessageHandler ForEvent<TEvent>(Action<TEvent> action) => this;
            public MessageHandler ForCommand<TCommand>(Action<TCommand> action) => this;
            public MessageHandler ForQuery<TQuery, TResult>(Func<TQuery, TResult> action) => this;
        }

        class MessageHandlerGroup
        {
            public MessageHandlerGroup(params Object[] children) {}
            public MessageHandlerGroup Add(MessageHandlerGroup child) => this;
            public MessageHandlerGroup Add(MessageHandler handler) => this;
        }

        class MessageHandlerGroupSettings
        {
            int MaximumThreads = 1;
        }

        class MessageHandlerSettings {}

        enum MessageHandlerGroupFlags
        {
            RunHandlersInParallelWithEachOther,
        }


        enum MessageHandlerFlags
        {
            HandleMessagesInParallel
        }

        class Endpoint
        {
            public Endpoint(params MessageHandlerGroup[] handlerGroups) { handlerGroups.ForEach(Add); }

            protected void Add(MessageHandlerGroup obj) {}
        }

        class AccountEndpoint : Endpoint
        {
            public AccountEndpoint()
            {
                Add(new MessageHandlerGroup(
                        new MessageHandler<AccountQueryModelUpdater>()
                            .ForEvent<AccountCreatedEvent>((handler, @event) => handler.Handle(@event)),
                        new MessageHandler<AccountCommandHandler>()
                            .ForCommand<CreateAccountCommand>((handler, command) => handler.Handle(command)),
                        new MessageHandler<AccountQueryHandler>()
                            .ForQuery<GetAccountQuery, string>((handler, query) => handler.Handle(query)),
                        new MessageHandler<AccountController>()
                            .ForEvent<AccountCreatedEvent>((handler, @event) => handler.Handle(@event))
                            .ForCommand<CreateAccountCommand>((handler, command) => handler.Handle(command))
                            .ForQuery<GetAccountQuery, string>((handler, query) => handler.Handle(query))
                    ));

                Add(new MessageHandlerGroup(
                        new MessageHandler()
                    ));
            }
        }

        class ForumsEndpoint : Endpoint {}

        class GlobalBus
        {
            public GlobalBus Register(params Endpoint[] endpoints) => this;
            public GlobalBus RegisterEndPoint<T>() => this;
        }

        class APiTest
        {
            void Setup()
            {
                var bus = new GlobalBus();

                //Use the type so that we can make use of injection in the configuration of the endpoints.
                bus.RegisterEndPoint<AccountEndpoint>()
                   .RegisterEndPoint<ForumsEndpoint>();

                var total = new MessageHandlerGroup(
                    MessageHandlerGroupFlags.RunHandlersInParallelWithEachOther,
                    new MessageHandlerGroup(
                        new MessageHandler()
                    ),
                    new MessageHandlerGroup()
                );
            }
        }

    }
}
