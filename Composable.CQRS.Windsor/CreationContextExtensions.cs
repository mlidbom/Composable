using System;
using System.Reflection;
using Castle.MicroKernel.Context;

namespace Composable.CQRS.Windsor
{
    public static  class CreationContextExtensions
    {
        [Obsolete("Please remove your Composable.CQRS.Windsor nuget package. These methods are now directly in the Composable.CQRS package.", error: true)]
        public static Type RequestingType(this CreationContext me)
        {
            return me.Handler.ComponentModel.Implementation;
        }

        [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
        public static Assembly RequestingAssembly(this CreationContext me)
        {
            return RequestingType(me).Assembly;
        }
    }
}