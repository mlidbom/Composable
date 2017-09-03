using System;
using System.Threading.Tasks;
using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi.Resources;

// ReSharper disable All
namespace Composable.HyperBus.DemoApp.Client
{
    public class D030AdvancedAwaiting
    {
        IApiNavigator<EmptyResource> Navigator { get; }


        public async Task DemoNavigatorUsageÁndAwaitingCascadeManually()
        {
            var apiNavigationSpecification = Navigator.Get(() => DemoApplicationApi.StartResource)
                                        .Get(start => start.Links.Accounts)
                                        .Execute(accounts => accounts.Commands.Register(email: "some@email.com", password: "secret"));

            var accountCreationCascade = await apiNavigationSpecification.RunAndReturnCascadeAsync();


            await accountCreationCascade.RunToEndOfActivation(); //Await complete activation
            await accountCreationCascade.RunToEndOfActivation(excludedEndpoints: new[] { Guid.Parse("13AF286B-1303-4028-A4FB-E32D7C456D99") }); //Wait for complete activation excepting the specified endpoints
            await accountCreationCascade.RunToEndOfActivation(includedEndpoints: new[] { Guid.Parse("13AF286B-1303-4028-A4FB-E32D7C456D99") }); //Only wait for specific endpoints to be done.            
            var account = await accountCreationCascade.GetReturnValue();
        }
    }
}