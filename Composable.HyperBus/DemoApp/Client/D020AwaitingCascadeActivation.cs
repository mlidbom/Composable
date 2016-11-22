using System;
using System.Threading.Tasks;
using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi.Resources;

namespace Composable.HyperBus.DemoApp.Client
{
    public class D020AwaitingCascadeActivation
    {
        private IApiNavigator<EmptyResource> Navigator { get; }

        public async Task DemoNavigatorUsageÁndAwaitingCascade()
        {
            var account = await Navigator.Get(() => DemoApplicationApi.StartResource)
                                        .Get(start => start.Links.Accounts)
                                        .Execute(accounts => accounts.Commands.Register(email: "some@email.com", password: "secret"))
                                        .RunAsync(NavigationOptions.AwaitCascadeActivationCompletion);
        }
    }
}