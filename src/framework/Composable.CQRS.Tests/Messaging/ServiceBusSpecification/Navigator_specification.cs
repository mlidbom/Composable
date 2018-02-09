using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

// ReSharper disable MemberCanBeMadeStatic.Local

namespace Composable.Tests.Messaging.ServiceBusSpecification
{
    public class Navigator_specification
    {
        public class Fixture : IDisposable
        {
            readonly ITestingEndpointHost _host;
            readonly IDisposable _scope;
            readonly IEndpoint _clientEndpoint;

            protected IRemoteApiNavigatorSession RemoteNavigator => _clientEndpoint.ServiceLocator.Resolve<IRemoteApiNavigatorSession>();

            public Fixture()
            {
                var queryResults = new List<UserResource>();

                _host = EndpointHost.Testing.Create(DependencyInjectionContainer.Create);

                _host.RegisterEndpoint(
                    "Backend",
                    new EndpointId(Guid.Parse("3A1B6A8C-D232-476C-A15A-9C8295413210")),
                    builder =>
                    {
                        builder.RegisterHandlers
                               .ForQuery((GetUserQuery query) => queryResults.Single(result => result.Name == query.Name))
                               .ForQuery((UserApiStartPageQuery query) => new UserApiStartPage())
                               .ForCommandWithResult((RegisterUserCommand command, IServiceBusSession bus) =>
                                {
                                    queryResults.Add(new UserResource(command.Name));
                                    return new UserRegisteredConfirmationResource(command.Name);
                                });

                        builder.TypeMapper
                               .Map<GetUserQuery>("44b8b0b6-fe09-4e3b-a22c-8d09bd51dbb0")
                               .Map<RegisterUserCommand>("ed799a31-0de9-41ae-ae7a-421438f2d857")
                               .Map<UserApiStartPageQuery>("4367ec6e-ddbc-42ea-91ad-9af1e6e4e29a")
                               .Map<UserRegisteredConfirmationResource>("c60604b2-2917-450b-bcbf-7d023065c005")
                               .Map<UserApiStartPage>("10b699df-35ac-430b-acb5-131df3cec5e1")
                               .Map<UserResource>("7e2c57ef-e079-4615-a402-1a76c70b5b0b");
                    });

                _clientEndpoint = _host.RegisterClientEndpoint();

                _host.Start();
                _scope = _clientEndpoint.ServiceLocator.BeginScope();
            }

            [Fact] void Can_get_command_result()
            {
                var commandResult1 = RemoteNavigator.Post(RegisterUserCommand.Create("new-user-name"));
                commandResult1.Name.Should().Be("new-user-name");
            }

            [Fact] void Can_navigate_to_startpage_execute_command_and_follow_command_result_link_to_the_created_resource()
            {
                var userResource = NavigationSpecification.Get(UserApiStartPage.Self)
                                                                .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                .Get(registerUserResult => registerUserResult.User).NavigateOn(RemoteNavigator);

                userResource.Name.Should().Be("new-user-name");
            }

            [Fact] async Task Can_navigate_async_to_startpage_execute_command_and_follow_command_result_link_to_the_created_resource()
            {
                var userResource = NavigationSpecification.Get(UserApiStartPage.Self)
                                                                .Post(startpage => startpage.RegisterUser("new-user-name"))
                                                                .Get(registerUserResult => registerUserResult.User)
                                                                .NavigateOnAsync(RemoteNavigator);

                (await userResource).Name.Should().Be("new-user-name");
            }

            public void Dispose()
            {
                _scope.Dispose();
                _host.Dispose();
            }

            class UserApiStartPage
            {
                public static UserApiStartPageQuery Self => new UserApiStartPageQuery();
                public RegisterUserCommand RegisterUser(string userName) => RegisterUserCommand.Create(userName);
            }

            protected class GetUserQuery : BusApi.Remotable.NonTransactional.Queries.Query<UserResource>
            {
                public GetUserQuery(string name) => Name = name;
                public string Name { get; private set; }
            }

            protected class UserResource
            {
                public UserResource(string name) => Name = name;
                public string Name { get; private set; }
            }

            protected class RegisterUserCommand : BusApi.Remotable.AtMostOnce.Command<UserRegisteredConfirmationResource>
            {
                RegisterUserCommand():base(MessageIdHandling.Reuse){ }

                public static RegisterUserCommand Create(string name) => new RegisterUserCommand()
                                                                         {
                                                                             Name = name,
                                                                             MessageId = Guid.NewGuid()
                                                                         };

                public string Name { get; private set;}
            }

            protected class UserRegisteredConfirmationResource
            {
                public UserRegisteredConfirmationResource(string name) => Name = name;
                public GetUserQuery User => new GetUserQuery(Name);
                public string Name { get; }
            }

            class UserApiStartPageQuery : BusApi.Remotable.NonTransactional.Queries.Query<UserApiStartPage> {}
        }
    }
}
