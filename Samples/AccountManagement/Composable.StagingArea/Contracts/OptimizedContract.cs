using System;

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
            return Arguments<object>(@params);
        }

        /// <summary>
        /// <para>Start inspecting a multiple unnamed arguments.</para>
        /// </summary>
        public Inspected<TParameter> Arguments<TParameter>(params TParameter[] @params)
        {
            //Yes the loops are not as pretty as a linq expression but this is performance critical code that might run in tight loops. If it was not I would be using linq.
            var inspected = new InspectedValue<TParameter>[@params.Length];
            for (var i = 0; i < @params.Length; i++)
            {
                inspected[i] = new InspectedValue<TParameter>(@params[i]);
            }
            return new Inspected<TParameter>(inspected);
        }

        ///<summary>Inspect a return value by passing in a Lambda that performs the inspections the same way you would for an argument.</summary>
        public static TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            assert(new Inspected<TReturnValue>(new InspectedValue<TReturnValue>(returnValue, "ReturnValue")));
            return returnValue;
        }   
    }
}