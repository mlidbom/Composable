using System;

namespace Composable.DependencyInjection
{
    static class ComponentRegistrationExtensions
    {
        public static Component.ComponentRegistrationBuilderWithInstantiationSpec<TService> UsingFactoryMethod<TService, TImplementation>(
            this Component.ComponentRegistrationBuilderInitial<TService> @this,
            Func<TImplementation> factoryMethod) where TService : class
                                                               where TImplementation : TService
        {
            return @this.UsingFactoryMethod(_ => factoryMethod());
        }

        public static Component.ComponentRegistrationBuilderWithInstantiationSpec<TService> UsingFactoryMethod<TService, TImplementation, TDependency1>(
            this Component.ComponentRegistrationBuilderInitial<TService> @this,
            Func<TDependency1, TImplementation> factoryMethod) where TService : class
                                                        where TDependency1 : class
                                                               where TImplementation : TService
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>()));
        }

        public static Component.ComponentRegistrationBuilderWithInstantiationSpec<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2>(
            this Component.ComponentRegistrationBuilderInitial<TService> @this,
            Func<TDependency1, TDependency2, TImplementation> factoryMethod) where TService : class
                                                                      where TDependency1 : class
                                                                      where TDependency2 : class
                                                                             where TImplementation : TService
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>()));
        }

        public static Component.ComponentRegistrationBuilderWithInstantiationSpec<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2, TDependency3>(
            this Component.ComponentRegistrationBuilderInitial<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TImplementation> factoryMethod) where TService : class
                                                                             where TDependency1 : class
                                                                             where TDependency2 : class
                                                                             where TImplementation : TService
                                                                                           where TDependency3 : class
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>()));
        }

        public static Component.ComponentRegistrationBuilderWithInstantiationSpec<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4>(
            this Component.ComponentRegistrationBuilderInitial<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TImplementation> factoryMethod) where TService : class
                                                                                           where TDependency1 : class
                                                                                           where TDependency2 : class
                                                                                           where TImplementation : TService
                                                                                           where TDependency3 : class
                                                                                                         where TDependency4 : class
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>()));
        }
    }
}
