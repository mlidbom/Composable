using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
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
            IDisposable _scope;

            public Fixture()
            {
                var queryResults = new List<UserResource>();

                Host = EndpointHost.Testing.BuildHost(
                    DependencyInjectionContainer.Create,
                    buildHost => buildHost.RegisterAndStartEndpoint(
                        "Backend",
                        new EndpointId(Guid.Parse("3A1B6A8C-D232-476C-A15A-9C8295413210")),
                        builder =>
                        {
                            builder.RegisterHandlers
                                   .ForEvent((UserRegisteredEvent myEvent) => queryResults.Add(new UserResource(myEvent.Name)))
                                   .ForQuery((GetUserQuery query) => queryResults.Single(result => result.Name == query.Name))
                                   .ForQuery((UserApiStartPageQuery query) => new UserApiStartPage())
                                   .ForCommandWithResult((RegisterUserCommand command, IApiBrowser bus) =>
                                    {
                                        bus.Publish(new UserRegisteredEvent(command.Name));
                                        return new UserRegisteredConfirmationResource(command.Name);
                                    });

                            builder.TypeMapper
                                   .Map<GetUserQuery>("44b8b0b6-fe09-4e3b-a22c-8d09bd51dbb0")
                                   .Map<RegisterUserCommand>("ed799a31-0de9-41ae-ae7a-421438f2d857")
                                   .Map<UserApiStartPageQuery>("4367ec6e-ddbc-42ea-91ad-9af1e6e4e29a")
                                   .Map<UserRegisteredEvent>("8a42968e-f18f-4126-9743-1a97cdd2ccab");
                        }));

                _scope = Host.ClientEndpoint.ServiceLocator.BeginScope();
            }

            [Fact] void Can_get_command_result()
            {
                var commandResult1 = Host.ClientBusSession.PostRemote(new RegisterUserCommand("new-user-name"));
                commandResult1.Name.Should().Be("new-user-name");
            }

            [Fact] void Can_navigate_to_startpage_execute_command_and_follow_command_result_link_to_the_created_resource()
            {
                var userResource = RemoteNavigationSpecification.GetRemote(UserApiStartPage.Self)
                                                                .PostRemote(startpage => startpage.RegisterUser("new-user-name"))
                                                                .GetRemote(registerUserResult => registerUserResult.User).ExecuteRemoteOn(Host.ClientBusSession);

                userResource.Name.Should().Be("new-user-name");
            }

            [Fact] async Task Can_navigate_async_to_startpage_execute_command_and_follow_command_result_link_to_the_created_resource()
            {
                var userResource = RemoteNavigationSpecification.GetRemote(UserApiStartPage.Self)
                                                                .PostRemote(startpage => startpage.RegisterUser("new-user-name"))
                                                                .GetRemote(registerUserResult => registerUserResult.User)
                                                                .ExecuteRemoteAsyncOn(Host.ClientBusSession);

                (await userResource).Name.Should().Be("new-user-name");
            }

            public void Dispose()
            {
                _scope.Dispose();
                Host.Dispose();
            }

            class UserApiStartPage : QueryResult
            {
                public static UserApiStartPageQuery Self => new UserApiStartPageQuery();
                public RegisterUserCommand RegisterUser(string userName) => new RegisterUserCommand(userName);
            }

            class UserRegisteredEvent : AggregateEvent
            {
                public UserRegisteredEvent(string name) => Name = name;
                public string Name { get; }
            }

            protected class GetUserQuery : BusApi.Remote.Query.RemoteQuery<UserResource>
            {
                public GetUserQuery(string name) => Name = name;
                public string Name { get; }
            }

            protected class UserResource : QueryResult
            {
                public UserResource(string name) => Name = name;
                public string Name { get; }
            }

            protected class RegisterUserCommand : BusApi.Remote.AtMostOnce.Command<UserRegisteredConfirmationResource>
            {
                public RegisterUserCommand(string name) => Name = name;
                public string Name { get; }
            }

            protected class UserRegisteredConfirmationResource : BusApi.Remote.ExactlyOnce.Message
            {
                public UserRegisteredConfirmationResource(string name) => Name = name;
                public GetUserQuery User => new GetUserQuery(Name);
                public string Name { get; }
            }

            class UserApiStartPageQuery : BusApi.Remote.Query.RemoteQuery<UserApiStartPage> {}
        }
    }
}
