using System;
using System.Linq;

namespace Composable.Contracts
{
    public class OptimizedContract
    {
        /// <summary>
        /// <para>Start inspecting a single argument and optionally pass its name as a string.</para>
        /// </summary>
        public Inspected<TParameter> Argument<TParameter>(TParameter argument, string name = "")
        {
            return new Inspected<TParameter>(argument, name);
        }

        /// <summary>
        /// <para>Start inspecting a multiple unnamed arguments.</para>
        /// </summary>
        public Inspected<object> Arguments(params object[] @params)
        {
            return new Inspected<object>(@params.Select(param => new InspectedValue<object>(param)).ToArray());
        }

        /// <summary>
        /// <para>Start inspecting a multiple unnamed arguments.</para>
        /// </summary>
        public Inspected<TParameter> Arguments<TParameter>(params TParameter[] @params)
        {
            return new Inspected<TParameter>(@params.Select(param => new InspectedValue<TParameter>(param)).ToArray());
        }

        ///<summary>Inspect a return value by passing in a Lambda that performs the inspections the same way you would for an argument.</summary>
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            assert(new Inspected<TReturnValue>(new InspectedValue<TReturnValue>(returnValue, "ReturnValue")));
            return returnValue;
        }   
    }
}