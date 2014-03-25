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
            return new Inspected<TParameter>(argument, InspectionType.Argument, name);
        }

        /// <summary>
        /// <para>Start inspecting a multiple unnamed arguments.</para>
        /// </summary>
        public Inspected<object> Arguments(params object[] @params)
        {
            return CreateInspected(@params, InspectionType.Argument);
        }

        /// <summary>
        /// <para>Start inspecting one or more unnamed arguments.</para>
        /// </summary>
        public Inspected<TParameter> Arguments<TParameter>(params TParameter[] @params)
        {
            return CreateInspected(@params, InspectionType.Argument);
        }

        /// <summary>
        /// <para>Start inspecting a single member and pass its name as a string.</para>
        /// </summary>
        public Inspected<TMember> Invariant<TMember>(TMember member, string name)
        {
            return new Inspected<TMember>(member, InspectionType.Invariant, name);
        }

        /// <summary>
        /// <para>Start inspecting a multiple unnamed arguments.</para>
        /// </summary>
        public Inspected<object> Invariant(params object[] members)
        {
            return CreateInspected(members, InspectionType.Invariant);
        }

        /// <summary>
        /// <para>Start inspecting one or more unnamed arguments.</para>
        /// </summary>
        public Inspected<TMember> Invariant<TMember>(params TMember[] members)
        {
            return CreateInspected(members, InspectionType.Invariant);
        }

        ///<summary>Start inspecting a return value</summary>
        public Inspected<TReturnValue> ReturnValue<TReturnValue>(TReturnValue returnValue)
        {
            return new Inspected<TReturnValue>(new InspectedValue<TReturnValue>(returnValue, InspectionType.ReturnValue, "ReturnValue"));
        }

        ///<summary>Lets you inspect and return a value in one statement by passing in a Lambda that performs the inspections the same way you would for an argument.</summary>
        public TReturnValue Return<TReturnValue>(TReturnValue returnValue, Action<Inspected<TReturnValue>> assert)
        {
            assert(ReturnValue(returnValue));
            return returnValue;
        }

        private static Inspected<TValue> CreateInspected<TValue>(TValue[] @params, InspectionType inspectionType)
        { //Yes the loop is not as pretty as a linq expression but this is performance critical code that might run in tight loops. If it was not I would be using linq.
            var inspected = new InspectedValue<TValue>[@params.Length];
            for(var i = 0; i < @params.Length; i++)
            {
                inspected[i] = new InspectedValue<TValue>(value: @params[i], type: inspectionType);
            }
            return new Inspected<TValue>(inspected);
        }
    }
}
