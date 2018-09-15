using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AccountManagement.API;
using AccountManagement.Domain;
using AccountManagement.Domain.Registration;
using AccountManagement.UserStories.Scenarios;
using Composable.DependencyInjection;
using Composable.Messaging;
using Composable.Messaging.Buses;
using Composable.System.Diagnostics;
using Composable.Testing.Performance;
using FluentAssertions.Extensions;
using NUnit.Framework;

namespace AccountManagement
{
    [TestFixture] class PerformanceTest
    {
        ITestingEndpointHost _host;
        IEndpoint _clientEndpoint;
        AccountScenarioApi _scenarioApi;

        [SetUp] public async Task SetupContainerAndBeginScope()
        {
            _host = EndpointHost.Testing.Create(DependencyInjectionContainer.Create);
            new AccountManagementServerDomainBootstrapper().RegisterWith(_host);
            _clientEndpoint = _host.RegisterClientEndpoint(setup: AccountApi.RegisterWithClientEndpoint);
            _scenarioApi = new AccountScenarioApi(_clientEndpoint);
            await _host.StartAsync();
            //Warmup
            StopwatchExtensions.TimeExecutionThreaded(() => _scenarioApi.Register.Execute(), iterations: 100, maxDegreeOfParallelism: 10);
        }

        [Test, Ignore("Only intended to be executed manually")] public void _1_threads_create_1000_accounts_in_5_second() =>
            TimeAsserter.Execute(() => _scenarioApi.Register.Execute(), iterations: 1000, maxTotal: 10.Seconds());

        [Test, Ignore("Only intended to be executed manually")] public void _10_threads_create_4000_accounts_in_8_seconds_then_logs_in_to_each_account_in_7_seconds_then_fetches_each_account_resource_in_zero_point_5_seconds()
        {
            var maxDegreeOfParallelism = 10;
            var accountCount = 4000;
            var accountsArray = new (string Email, string Password, Guid Id)[accountCount];

            var currentAccount = -1;

            TimeAsserter.ExecuteThreaded(description: "Register account",
                                         setup: () => currentAccount = -1,
                                         action: () =>
                                         {
                                             var registerAccountScenario = _scenarioApi.Register;
                                             var account = registerAccountScenario.Execute();
                                             if(account.Result.Status != RegistrationAttemptStatus.Successful)
                                             {
                                                 throw new Exception(account.Result.Status.ToString());
                                             }

                                             accountsArray[Interlocked.Increment(ref currentAccount)] = (registerAccountScenario.Email, registerAccountScenario.Password, registerAccountScenario.AccountId);
                                         },
                                         iterations: accountCount,
                                         maxTotal: 8.Seconds(),
                                         maxDegreeOfParallelism: maxDegreeOfParallelism);

            TimeAsserter.ExecuteThreaded(description: "Log in to account",
                                         setup: () => currentAccount = -1,
                                         action: () =>
                                         {
                                             var account = accountsArray[Interlocked.Increment(ref currentAccount)];

                                             var loginResult = _scenarioApi.Login(account.Email, account.Password).Execute();
                                             if(!loginResult.Succeeded)
                                             {
                                                 throw new Exception();
                                             }
                                         },
                                         iterations: accountCount,
                                         maxTotal: 7.Seconds(),
                                         maxDegreeOfParallelism: maxDegreeOfParallelism);

            TimeAsserter.ExecuteThreaded(description: "Fetch account resource",
                                         setup: () => currentAccount = -1,
                                         action: () =>
                                         {
                                             var account = accountsArray[Interlocked.Increment(ref currentAccount)];

                                             var accountResource = _clientEndpoint.ExecuteRequest(AccountApi.Instance.Query.AccountById(account.Id));
                                             if(accountResource.Id != account.Id)
                                             {
                                                 throw new Exception();
                                             }
                                         },
                                         iterations: accountCount,
                                         maxTotal: 0.5.Seconds(),
                                         maxDegreeOfParallelism: maxDegreeOfParallelism);
        }

        [TearDown] public void Teardown() => _host.Dispose();
    }
}
