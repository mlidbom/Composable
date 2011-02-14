using System;
using Microsoft.Practices.ServiceLocation;
using System.Linq;
using Composable.System.Linq;

namespace Composable.CQRS
{
    public static class ServiceLocatorExtensions
    {
        public static T GetSingleInstance<T>(this IServiceLocator me)
        {
            var instances = me.GetAllInstances<T>();
            if(instances.Count() > 1)
            {
                throw new DuplicateHandlersException(typeof(T), instances.Cast<Object>());
            }
            if(instances.None())
            {
                throw new NoRegisteredHandlersException(typeof(T));
            }
            return instances.Single();
        }
    }
}