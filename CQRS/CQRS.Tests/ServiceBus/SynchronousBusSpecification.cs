using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Composable.ServiceBus;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
using NServiceBus;

namespace CQRS.Tests.ServiceBus
{
    public class SynchronousBusSpecification : NSpec.NUnit.nspec
    {
        public void given_no_registered_handlers()
        {
            WindsorContainer container = null;
            before = () =>
                     {
                         container = new WindsorContainer();
                         container.Register(
                             Component.For<ISingleContextUseGuard>().ImplementedBy<SingleThreadUseGuard>(),
                             Component.For<SynchronousBus>(),
                             Component.For<IWindsorContainer>().Instance(container)
                             );
                     };
            Func<SynchronousBus> getBus = () => container.Resolve<SynchronousBus>();
            it["Handles(new AMessage()) returns false"] = () => getBus().Handles(new AMessage()).Should().Be(false);
            it["Send(new AMessage()) throws NoHandlerException"] = expect<NoHandlerException>(() => getBus().Send(new AMessage()));
            it["SendLocal(new AMessage()) throws NoHandlerException"] = expect<NoHandlerException>(() => getBus().SendLocal(new AMessage()));

            //Todo:reply should throw an exception telling us that you cannot reply except while handling a message
            //it["Reply(new AMessage()) throws CantCallReplyWhenNotHandlingMessageException"] = expect<CantCallReplyWhenNotHandlingMessageException>(() => getBus().Reply(new AMessage()));

            it["Publish(new AMessage()) throws no exception"] = () => getBus().Publish(new AMessage());

            context["after registering AMessageHandler as handler for AMessage in container"] =
                () =>
                {
                    before = () => container.Register(
                        Component.For<AMessageHandler, IHandleMessages<AMessage>>().ImplementedBy<AMessageHandler>()
                        );
                    it["Handles(new AMessage()) returns true"] = () => getBus().Handles(new AMessage()).Should().Be(true);
                    it["Publish(new AMessage()) throws no exception"] = () => getBus().Publish(new AMessage());
                    it["Publish(new AMessage()) dispatches to AMessageHandler"] = () =>
                                                                                  {
                                                                                      getBus().Publish(new AMessage());
                                                                                      container.Resolve<AMessageHandler>().ReceivedMessage.Should().Be(true);
                                                                                  };
                    it["Send(new AMessage()) dispatches to AMessageHandler"] = () =>
                                                                               {
                                                                                   getBus().Send(new AMessage());
                                                                                   container.Resolve<AMessageHandler>().ReceivedMessage.Should().Be(true);
                                                                               };
                    it["SendLocal(new AMessage()) dispatches to AMessageHandler"] = () =>
                                                                                    {
                                                                                        getBus().SendLocal(new AMessage());
                                                                                        container.Resolve<AMessageHandler>().ReceivedMessage.Should().Be(true);
                                                                                    };

                    //Todo:reply should throw an exception telling us that you cannot reply except while handling a message
                    //it["Reply(new AMessage()) throws CantCallReplyWhenNotHandlingMessageException"] = expect<CantCallReplyWhenNotHandlingMessageException>(() => getBus().Reply(new AMessage()));

                    context["after registering a SynchrounousBusSubscriberFilter that excludes AMessageHandler"] =
                        () =>
                        {
                            before = () => container.Register(Component.For<ISynchronousBusSubscriberFilter>().ImplementedBy<FilterAMessageHandlerSubscriberFilter>());
                            it["Handles(new AMessage()) returns true"] = () => getBus().Handles(new AMessage()).Should().Be(true);
                            it["Publish(new AMessage()) does not dispatch to AMessageHandler"] = () =>
                                                                                                 {
                                                                                                     getBus().Publish(new AMessage());
                                                                                                     container.Resolve<AMessageHandler>().ReceivedMessage.Should().Be(false);
                                                                                                 };
                            it["Send(new AMessage()) dispatches to AMessageHandler"] = () =>
                                                                                       {
                                                                                           getBus().Send(new AMessage());
                                                                                           container.Resolve<AMessageHandler>().ReceivedMessage.Should().Be(true);
                                                                                       };

                            it["SendLocal(new AMessage()) dispatches to AMessageHandler"] = () =>
                                                                                            {
                                                                                                getBus().SendLocal(new AMessage());
                                                                                                container.Resolve<AMessageHandler>().ReceivedMessage.Should().Be(true);
                                                                                            };
                        };
                };
        }

        public class AMessage : IMessage {}

        public class AMessageHandler : IHandleMessages<AMessage>
        {
            public bool ReceivedMessage;

            public void Handle(AMessage message)
            {
                ReceivedMessage = true;
            }
        }
    }

    public class FilterAMessageHandlerSubscriberFilter : ISynchronousBusSubscriberFilter
    {
        public bool PublishMessageToHandler(object message, object handler)
        {
            return handler.GetType() != typeof(SynchronousBusSpecification.AMessageHandler);
        }
    }
}
