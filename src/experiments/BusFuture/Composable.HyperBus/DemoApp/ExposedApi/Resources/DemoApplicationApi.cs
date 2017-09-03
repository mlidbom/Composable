using Composable.HyperBus.APIDraft;

// ReSharper disable All
namespace Composable.HyperBus.DemoApp.ExposedApi.Resources
{
    public class DemoApplicationApi
    {
        public static IQuery<DemoApplicationStartResource> StartResource { get; }
    }
}