using System;

namespace Composable.DependencyInjection
{
    public static class NewComponentRegistrationExtensions
    {
        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TImplementation> factoryMethod) where TService : class
                                                 where TImplementation : TService
        {
            return @this.UsingFactoryMethod(_ => factoryMethod());
        }

        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation, TDependency1>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TImplementation> factoryMethod) where TService : class
                                                               where TDependency1 : class
                                                               where TImplementation : TService
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>()));
        }

        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TImplementation> factoryMethod) where TService : class
                                                                             where TDependency1 : class
                                                                             where TDependency2 : class
                                                                             where TImplementation : TService
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>()));
        }

        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2, TDependency3>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TImplementation> factoryMethod) where TImplementation : TService
                                                                                           where TService : class
                                                                                           where TDependency1 : class
                                                                                           where TDependency2 : class
                                                                                           where TDependency3 : class
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>()));
        }

        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                         where TService : class
                                                                                                         where TDependency1 : class
                                                                                                         where TDependency2 : class
                                                                                                         where TDependency3 : class
                                                                                                         where TDependency4 : class
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>()));
        }

        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                       where TService : class
                                                                                                                       where TDependency1 : class
                                                                                                                       where TDependency2 : class
                                                                                                                       where TDependency3 : class
                                                                                                                       where TDependency4 : class
                                                                                                                       where TDependency5 : class
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>()));
        }

        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                     where TService : class
                                                                                                                                     where TDependency1 : class
                                                                                                                                     where TDependency2 : class
                                                                                                                                     where TDependency3 : class
                                                                                                                                     where TDependency4 : class
                                                                                                                                     where TDependency5 : class
                                                                                                                                     where TDependency6 : class
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>()));
        }

        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                   where TService : class
                                                                                                                                                   where TDependency1 : class
                                                                                                                                                   where TDependency2 : class
                                                                                                                                                   where TDependency3 : class
                                                                                                                                                   where TDependency4 : class
                                                                                                                                                   where TDependency5 : class
                                                                                                                                                   where TDependency6 : class
                                                                                                                                                   where TDependency7 : class
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>()));
        }

        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                 where TService : class
                                                                                                                                                                 where TDependency1 : class
                                                                                                                                                                 where TDependency2 : class
                                                                                                                                                                 where TDependency3 : class
                                                                                                                                                                 where TDependency4 : class
                                                                                                                                                                 where TDependency5 : class
                                                                                                                                                                 where TDependency6 : class
                                                                                                                                                                 where TDependency7 : class
                                                                                                                                                                 where TDependency8 : class
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>()));
        }

        public static ComponentRegistration<TService> UsingFactoryMethod<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9>(
            this Component.ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                                                               where TService : class
                                                                                                                                                                               where TDependency1 : class
                                                                                                                                                                               where TDependency2 : class
                                                                                                                                                                               where TDependency3 : class
                                                                                                                                                                               where TDependency4 : class
                                                                                                                                                                               where TDependency5 : class
                                                                                                                                                                               where TDependency6 : class
                                                                                                                                                                               where TDependency7 : class
                                                                                                                                                                               where TDependency8 : class
                                                                                                                                                                               where TDependency9 : class
        {
            return @this.UsingFactoryMethod(kern => factoryMethod(kern.Resolve<TDependency1>(), kern.Resolve<TDependency2>(), kern.Resolve<TDependency3>(), kern.Resolve<TDependency4>(), kern.Resolve<TDependency5>(), kern.Resolve<TDependency6>(), kern.Resolve<TDependency7>(), kern.Resolve<TDependency8>(), kern.Resolve<TDependency9>()));
        }
    }
}
