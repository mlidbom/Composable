using System;
using System.Reflection;
using Castle.MicroKernel.Context;

namespace Composable.CQRS.Windsor
{
    public static  class CreationContextExtensions
    {
        public static Type RequestingType(this CreationContext me)
        {
            return me.Handler.ComponentModel.Implementation;
        }

        public static Assembly RequestingAssembly(this CreationContext me)
        {
            return me.RequestingType().Assembly;
        }
    }
}