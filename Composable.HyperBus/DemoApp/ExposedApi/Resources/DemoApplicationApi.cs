using Composable.HyperBus.APIDraft;

namespace Composable.HyperBus.DemoApp.ExposedApi.Resources
{
    public class DemoApplicationApi
    {
        public static IQuery<DemoApplicationStartResource> StartResource { get; }
    }
}