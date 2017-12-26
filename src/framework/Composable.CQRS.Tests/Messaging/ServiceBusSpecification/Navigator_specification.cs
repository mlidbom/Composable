using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.Messaging.Commands;
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
                        builder => builder.RegisterHandlers
                                          .ForEvent((UserRegisteredEvent myEvent) => queryResults.Add(new UserResource(myEvent.Name)))
                                          .ForQuery((GetUserQuery query) => queryResults.Single(result => result.Name == query.Name))
                                          .ForQuery((UserApiStartPageQuery query) => new UserApiStartPage())
                                          .ForCommandWithResult((RegisterUserCommand command, IServiceBus bus) =>
                                          {
                                              bus.PublishAsync(new UserRegisteredEvent(command.Name));
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

            class UserRegisteredEvent : Event
            {
                public UserRegisteredEvent(string name) => Name = name;
                public string Name { get; }
            }

            protected class GetUserQuery : Query<UserResource>
            {
                public GetUserQuery(string name) => Name = name;
                public string Name { get; }
            }

            protected class UserResource : QueryResult
            {
                public UserResource(string name) => Name = name;
                public string Name { get; }
            }

            protected class RegisterUserCommand : DomainCommand<UserRegisteredConfirmationResource>
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

            class UserApiStartPageQuery : Query<UserApiStartPage> {}
        }
    }
}
