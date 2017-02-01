using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi.Resources.Accounts;

// ReSharper disable All
namespace Composable.HyperBus.DemoApp.ExposedApi.Resources
{
    public class DemoApplicationStartResource
    {
        public LinksClass Links { get; } = new LinksClass();
        public class LinksClass
        {
            public IQuery<AccountsStartResource> Accounts { get; }
        }
    }
}