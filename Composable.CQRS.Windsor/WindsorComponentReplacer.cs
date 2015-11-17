using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;

namespace Composable.CQRS.Windsor
{
    [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
    public static class WindsorComponentReplacer
    {
        [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
        public static IWindsorContainer ReplaceComponent<TServiceType>(this IWindsorContainer @this, string componentName, ComponentRegistration<TServiceType> replacement, string replacementName = null) where TServiceType : class
        {
            replacementName = replacementName ?? Guid.NewGuid().ToString();
            if (!@this.Kernel.HasComponent(replacementName))
            {
                @this.Register(replacement.Named(replacementName));
            }

            @this.Kernel.AddHandlerSelector(
                new KeyReplacementHandlerSelector(
                    serviceType: typeof(TServiceType),
                    originalKey: componentName,
                    replacementKey: replacementName));

            return @this;
        }

        [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
        public static IWindsorContainer ReplaceDefault<TServiceType>(this IWindsorContainer @this, ComponentRegistration<TServiceType> replacement) where TServiceType : class
        {
            var replacementName = Guid.NewGuid().ToString();

            @this.Register(replacement.Named(replacementName));

            @this.Kernel.AddHandlerSelector(
                new DefaultToKeyHandlerSelector(
                    type: typeof(TServiceType),
                    keyToDefaultTo: replacementName));

            return @this;
        }          
    }
}
