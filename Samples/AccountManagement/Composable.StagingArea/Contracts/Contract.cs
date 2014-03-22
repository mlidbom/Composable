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

        public static InspectionTarget<TParameter> Argument<TParameter>(params TParameter[] param)
        {
            return new InspectionTarget<TParameter>(param);
        }

        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<InspectionTarget<TReturnValue>> assert)
        {
            assert(new InspectionTarget<TReturnValue>(returnValue));
            return returnValue;
        }
    }
}
// ReSharper restore UnusedParameter.Global