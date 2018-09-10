using System.Linq;

namespace Composable.DependencyInjection
{
    static class ComponentRegistrationExtensionsOptionalRegistration
    {
        public static bool HasComponent<TComponent>(this IDependencyInjectionContainer @this) =>
            @this.RegisteredComponents().Any(component => component.ServiceTypes.Contains(typeof(TComponent)));
    }
}
