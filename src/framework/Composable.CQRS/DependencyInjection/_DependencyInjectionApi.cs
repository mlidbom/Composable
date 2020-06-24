using System;
using System.Collections.Generic;

namespace Composable.DependencyInjection
{
    public interface IDependencyInjectionContainer : IDisposable
    {
        IRunMode RunMode { get; }
        void Register(params ComponentRegistration[] registrations);
        IEnumerable<ComponentRegistration> RegisteredComponents();
        IServiceLocator CreateServiceLocator();
    }

    public interface IServiceLocator : IDisposable
    {
        TComponent Resolve<TComponent>() where TComponent : class;
        TComponent[] ResolveAll<TComponent>() where TComponent : class;
        IDisposable BeginScope();
    }

    interface IServiceLocatorKernel
    {
        TComponent Resolve<TComponent>() where TComponent : class;
    }

    public interface IRunMode
    {
        bool IsTesting { get; }
        //urgent: TestingMode should no longer be used. Just the current PersistenceLayerProvider
        TestingMode TestingMode { get; }
        PersistenceLayer TestingPersistenceLayer { get; }
    }

    public enum PersistenceLayer
    {
        SqlServer,
        InMemory,
        MySql
    }

    //urgent: IsTesting and TestingMode should no longer be used. Just the current PersistenceLayerProvider
    public enum TestingMode
    {
        InMemory,
        DatabasePool
    }

    enum Lifestyle
    {
        Singleton,
        Scoped
    }
}
