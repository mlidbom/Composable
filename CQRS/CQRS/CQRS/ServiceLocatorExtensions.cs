#region usings

using System;
using System.Diagnostics.Contracts;
using System.Linq;
using Composable.System.Linq;
using Microsoft.Practices.ServiceLocation;

#endregion

namespace Composable.CQRS
{
    public static class ServiceLocatorExtensions
    {
        public static T GetSingleInstance<T>(this IServiceLocator me)
        {
            Contract.Requires(me != null);
            Contract.Ensures(Contract.Result<T>() != null);
            var instances = me.GetAllInstances<T>().ToArray();
            Contract.Assume(instances != null);
            if(instances.Count() > 1)
            {
                throw new DuplicateHandlersException(typeof(T), instances.Cast<Object>());
            }
            if(instances.None())
            {
                throw new NoRegisteredHandlersException(typeof(T));
            }
            return instances.SingleNotNull();
        }
    }
}