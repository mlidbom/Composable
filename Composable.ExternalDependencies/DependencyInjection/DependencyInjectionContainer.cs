﻿using System;
using Composable.DependencyInjection.Testing;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class DependencyInjectionContainer
    {
        internal static IRunMode RunMode(this IDependencyInjectionContainer @this) => @this.CreateServiceLocator()
                                                                                  .Resolve<IRunMode>();

        public static IServiceLocator CreateServiceLocatorForTesting([InstantHandle]Action<IDependencyInjectionContainer> setup, TestingMode mode = TestingMode.RealComponents)
        {
            var @this = Create();


            @this.ConfigureWiringForTestsCallBeforeAllOtherWiring(mode);

            setup(@this);

            return @this.CreateServiceLocator();
        }

        internal static IDependencyInjectionContainer Create() => new Windsor.WindsorDependencyInjectionContainer();
    }
}