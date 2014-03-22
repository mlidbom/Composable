using System;
using System.Linq;

// ReSharper disable UnusedParameter.Global

namespace Composable.Contracts
{
    public static class Contract
    {       
        public static void ArgumentNotNull(params object[] arguments)            
        {
            if(arguments.Any(argument => argument == null))
            {
                throw new ArgumentNullException();
            }
        }

        public static Inspected<TParameter> Argument<TParameter>(TParameter argument, string name = "")
        {
            return new Inspected<TParameter>(argument, name);
        }

        public static Inspected<TParameter> Arguments<TParameter>(params TParameter[] @params)
        {
            return new Inspected<TParameter>(@params.Select(param => new InspectedValue<TParameter>(param)).ToArray());
        }

        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            assert(new Inspected<TReturnValue>(new InspectedValue<TReturnValue>(returnValue, "ReturnValue")));
            return returnValue;
        }
    }
}
// ReSharper restore UnusedParameter.Global