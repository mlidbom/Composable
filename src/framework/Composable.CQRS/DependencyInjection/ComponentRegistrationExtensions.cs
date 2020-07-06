using System;

namespace Composable.DependencyInjection
{
    public static class ComponentRegistrationExtensions
    {
        class ComponentPromise<TService> where TService : class
        {
            class ComposableState
            {
                public ComposableState(TService? singletonInstance, ComponentRegistration registration, IServiceLocatorKernel initialKernel, Lifestyle lifestyle)
                {
                    SingletonInstance = singletonInstance;
                    Registration = registration;
                    InitialKernel = initialKernel;
                    Lifestyle = lifestyle;
                }

                public readonly TService? SingletonInstance;
                public readonly ComponentRegistration Registration;
                public readonly IServiceLocatorKernel InitialKernel;
                public readonly Lifestyle Lifestyle;
            }

            readonly object _lock = new object();
            bool _unInitialized = true;
            ComposableState? _composableState;
            TService? _singletonInstance;
            public TService Resolve(IServiceLocatorKernel kernel)
            {
                if(_unInitialized)
                {
                    lock(_lock)
                    {
                        if(_unInitialized)
                        {
                            //refactor: We should have no type checking in here. Why would this be used for other kernels?
                            if(kernel is ComposableDependencyInjectionContainer container)
                            {
                                var registration = container.GetRegistrationFor<TService>();
                                switch(registration.Lifestyle)
                                {
                                    case Lifestyle.Singleton:
                                        _singletonInstance = container.ResolveSingleton<TService>(registration);
                                        break;
                                    case Lifestyle.Scoped:
                                        //performance: Custom method for resolving scoped components.
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                _composableState = new ComposableState(_singletonInstance, registration, kernel, registration.Lifestyle);
                            }

                            _unInitialized = false;
                        }
                    }
                }

                if(_composableState is null)
                    return kernel.Resolve<TService>();

                switch(_composableState.Lifestyle)
                {
                    case Lifestyle.Singleton:
                        if(_composableState.InitialKernel == kernel)
                        {
                            return _composableState.SingletonInstance!;
                        }
                        return ((ComposableDependencyInjectionContainer)kernel).ResolveSingleton<TService>(_composableState.Registration);
                    case Lifestyle.Scoped:
                        return ((ComposableDependencyInjectionContainer)kernel).ResolveScoped<TService>(_composableState.Registration);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TImplementation> factoryMethod) where TService : class
                                                 where TImplementation : TService
        {
            return @this.CreatedBy(_ => factoryMethod());
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TImplementation> factoryMethod) where TService : class
                                                               where TDependency1 : class
                                                               where TImplementation : TService
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TImplementation> factoryMethod) where TService : class
                                                                             where TDependency1 : class
                                                                             where TDependency2 : class
                                                                             where TImplementation : TService
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TImplementation> factoryMethod) where TImplementation : TService
                                                                                           where TService : class
                                                                                           where TDependency1 : class
                                                                                           where TDependency2 : class
                                                                                           where TDependency3 : class
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            var dependency3 = new ComponentPromise<TDependency3>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                         where TService : class
                                                                                                         where TDependency1 : class
                                                                                                         where TDependency2 : class
                                                                                                         where TDependency3 : class
                                                                                                         where TDependency4 : class
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            var dependency3 = new ComponentPromise<TDependency3>();
            var dependency4 = new ComponentPromise<TDependency4>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                       where TService : class
                                                                                                                       where TDependency1 : class
                                                                                                                       where TDependency2 : class
                                                                                                                       where TDependency3 : class
                                                                                                                       where TDependency4 : class
                                                                                                                       where TDependency5 : class
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            var dependency3 = new ComponentPromise<TDependency3>();
            var dependency4 = new ComponentPromise<TDependency4>();
            var dependency5 = new ComponentPromise<TDependency5>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
            Func<TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TImplementation> factoryMethod) where TImplementation : TService
                                                                                                                                     where TService : class
                                                                                                                                     where TDependency1 : class
                                                                                                                                     where TDependency2 : class
                                                                                                                                     where TDependency3 : class
                                                                                                                                     where TDependency4 : class
                                                                                                                                     where TDependency5 : class
                                                                                                                                     where TDependency6 : class
        {
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            var dependency3 = new ComponentPromise<TDependency3>();
            var dependency4 = new ComponentPromise<TDependency4>();
            var dependency5 = new ComponentPromise<TDependency5>();
            var dependency6 = new ComponentPromise<TDependency6>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
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
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            var dependency3 = new ComponentPromise<TDependency3>();
            var dependency4 = new ComponentPromise<TDependency4>();
            var dependency5 = new ComponentPromise<TDependency5>();
            var dependency6 = new ComponentPromise<TDependency6>();
            var dependency7 = new ComponentPromise<TDependency7>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
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
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            var dependency3 = new ComponentPromise<TDependency3>();
            var dependency4 = new ComponentPromise<TDependency4>();
            var dependency5 = new ComponentPromise<TDependency5>();
            var dependency6 = new ComponentPromise<TDependency6>();
            var dependency7 = new ComponentPromise<TDependency7>();
            var dependency8 = new ComponentPromise<TDependency8>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern)));
        }

        public static ComponentRegistration<TService> CreatedBy<TService, TImplementation, TDependency1, TDependency2, TDependency3, TDependency4, TDependency5, TDependency6, TDependency7, TDependency8, TDependency9>(
            this ComponentRegistrationWithoutInstantiationSpec<TService> @this,
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
            var dependency1 = new ComponentPromise<TDependency1>();
            var dependency2 = new ComponentPromise<TDependency2>();
            var dependency3 = new ComponentPromise<TDependency3>();
            var dependency4 = new ComponentPromise<TDependency4>();
            var dependency5 = new ComponentPromise<TDependency5>();
            var dependency6 = new ComponentPromise<TDependency6>();
            var dependency7 = new ComponentPromise<TDependency7>();
            var dependency8 = new ComponentPromise<TDependency8>();
            var dependency9 = new ComponentPromise<TDependency9>();
            return @this.CreatedBy(kern => factoryMethod(dependency1.Resolve(kern), dependency2.Resolve(kern), dependency3.Resolve(kern), dependency4.Resolve(kern), dependency5.Resolve(kern), dependency6.Resolve(kern), dependency7.Resolve(kern), dependency8.Resolve(kern), dependency9.Resolve(kern)));
        }
    }
}
