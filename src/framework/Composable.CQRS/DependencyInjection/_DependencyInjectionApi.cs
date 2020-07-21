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
    }

    public enum PersistenceLayer
    {
        MsSql,
        Memory,
        MySql,
        PgSql,
        Orcl,
        DB2
    }

    public enum DIContainer
    {
        Com, Sim, Win
    }

    enum Lifestyle
    {
        Singleton,
        Scoped
    }
}
