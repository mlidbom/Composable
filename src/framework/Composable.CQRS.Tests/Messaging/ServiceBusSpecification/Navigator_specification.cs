using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
using Composable.Persistence.EventStore;
using FluentAssertions;
using Xunit;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Composable.Tests.Messaging.ServiceBusSpecification
{
    public class Navigator_specification
    {
        public class Fixture : IDisposable
        {
            readonly ITestingEndpointHost Host;

            public Fixture()
            {
                var queryResults = new List<UserResource>();

                Host = EndpointHost.Testing.BuildHost(
                    DependencyInjectionContainer.Create,
                    buildHost => buildHost.RegisterAndStartEndpoint(
                        "Backend",
                        new EndpointId(Guid.Parse("3A1B6A8C-D232-476C-A15A-9C8295413210")),
                        builder => builder.RegisterHandlers
                                          .ForEvent((UserRegisteredEvent myEvent) => queryResults.Add(new UserResource(myEvent.Name)))
                                          .ForQuery((GetUserQuery query) => queryResults.Single(result => result.Name == query.Name))
                                          .ForQuery((UserApiStartPageQuery query) => new UserApiStartPage())
                                          .ForCommandWithResult((RegisterUserCommand command, IServiceBus bus) =>
                                          {
                                              bus.Publish(new UserRegisteredEvent(command.Name));
                                              return new UserRegisteredConfirmationResource(command.Name);
                                          })));
            }

            [Fact] void Can_get_command_result()
            {
                var commandResult1 = Host.ClientBus.Post(new RegisterUserCommand("new-user-name")).Execute();
                commandResult1.Name.Should().Be("new-user-name");
            }

            [Fact] void Can_navigate_to_startpage_execute_command_and_follow_command_result_link_to_the_created_resource()
            {
                var userResource = Host.ClientBus
                                       .Get(UserApiStartPage.Self)
                                       .Post(startpage => startpage.RegisterUser("new-user-name"))
                                       .Get(registerUserResult => registerUserResult.User)
                                       .Execute();

                userResource.Name.Should().Be("new-user-name");
            }

            [Fact] async Task Can_navigate_async_to_startpage_execute_command_and_follow_command_result_link_to_the_created_resource()
            {
                var userResource = Host.ClientNavigator
                                       .Get(UserApiStartPage.Self)
                                       .Post(startpage => startpage.RegisterUser("new-user-name"))
                                       .Get(registerUserResult => registerUserResult.User)
                                       .ExecuteAsync();

                (await userResource).Name.Should().Be("new-user-name");
            }

            public void Dispose() { Host.Dispose(); }

            class UserApiStartPage : QueryResult
            {
                public static UserApiStartPageQuery Self => new UserApiStartPageQuery();
                public RegisterUserCommand RegisterUser(string userName) => new RegisterUserCommand(userName);
            }

            [TypeId("6EDCBCD0-C1DE-4499-9CBB-8E8E8405A9C3")]class UserRegisteredEvent : DomainEvent
            {
                public UserRegisteredEvent(string name) => Name = name;
                public string Name { get; }
            }

            [TypeId("3D2C5363-620E-4859-BF94-9535BCC994FA")]protected class GetUserQuery : Query<UserResource>
            {
                public GetUserQuery(string name) => Name = name;
                public string Name { get; }
            }

            protected class UserResource : QueryResult
            {
                public UserResource(string name) => Name = name;
                public string Name { get; }
            }

            [TypeId("3B1A2A50-F114-4886-B981-C56753AFD55E")]protected class RegisterUserCommand : TransactionalExactlyOnceDeliveryCommand<UserRegisteredConfirmationResource>
            {
                public RegisterUserCommand(string name) => Name = name;
                public string Name { get; }
            }

            protected class UserRegisteredConfirmationResource : Message
            {
                public UserRegisteredConfirmationResource(string name) => Name = name;
                public GetUserQuery User => new GetUserQuery(Name);
                public string Name { get; }
            }

            [TypeId("F762C93B-8F03-4A1C-BCCA-DC43A5EC4459")]class UserApiStartPageQuery : Query<UserApiStartPage> {}
        }
    }
}
