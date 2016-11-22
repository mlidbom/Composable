using System;
using System.Threading.Tasks;
using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi.Resources;

namespace Composable.HyperBus.DemoApp.Client
{
    public class D01NavigatorUsage
    {
        private IApiNavigator<EmptyResource> Navigator { get; }

        public async Task DemoNavigatorUsage()
        {
            var account = await Navigator.Get(() => DemoApplicationApi.StartResource)
                                        .Get(start => start.Links.Accounts)
                                        .Execute(accounts => accounts.Commands.Register(email: "some@email.com", password: "secret"))
                                        .RunAsync();

            var contact = await Navigator.Get(() => account.Links.Contact).RunAsync();

            await Navigator.Execute(() => account.Commands.ChangeEmail("new@email.com")).RunAsync();
        }
    }
}