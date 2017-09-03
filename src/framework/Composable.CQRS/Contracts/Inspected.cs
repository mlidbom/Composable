using System;

namespace Composable.Contracts
{
    public interface IInspected<TValue> {
        /// <summary>
        /// Performs the supplied inspection against each <see cref="InspectedValue"/> in the instance.
        /// </summary>
        /// <param name="isValueValid">Expression that should return true if the <see cref="InspectedValue{TValue}"/> is valid. </param>
        /// <param name="buildException">Expression that should return an appropriate exception if the inspection fails. If not supplied a default <see cref="ContractViolationException"/> vill be created.</param>
        /// <returns>The same instance (this) in order to enable fluent chaining style code.</returns>
        /// <exception cref="Exception">The exception created by the buildException argument will be thrown if an <see cref="InspectedValue{TValue}"/> fails inspection.</exception>
        IInspected<TValue> Inspect(Func<TValue, bool> isValueValid, Func<IInspectedValue<TValue>, Exception> buildException = null);
    }

    class Inspected<TValue> : IInspected<TValue>
    {
        readonly IInspectedValue<TValue>[] _inspectedValues;

        /// <summary>
        /// Performs the supplied inspection against each <see cref="InspectedValue"/> in the instance.
        /// </summary>
        /// <param name="isValueValid">Expression that should return true if the <see cref="InspectedValue{TValue}"/> is valid. </param>
        /// <param name="buildException">Expression that should return an appropriate exception if the inspection fails. If not supplied a default <see cref="ContractViolationException"/> vill be created.</param>
        /// <returns>The same instance (this) in order to enable fluent chaining style code.</returns>
        /// <exception cref="Exception">The exception created by the buildException argument will be thrown if an <see cref="InspectedValue{TValue}"/> fails inspection.</exception>
        public IInspected<TValue> Inspect(Func<TValue, bool> isValueValid, Func<IInspectedValue<TValue>, Exception> buildException = null)
        {
            if(buildException == null)
            {
                buildException = badValue => new ContractViolationException(badValue);
            }

            //Yes the loop is not as pretty as a linq expression but this is performance critical code that might run in tight loops. If it was not I would be using linq.
            foreach(var inspected in _inspectedValues)
            {
                if(!isValueValid(inspected.Value))
                {
                    throw buildException(inspected);
                }
            }
            return this;
        }

        ///<summary>Standard constructor</summary>
        internal Inspected(params IInspectedValue<TValue>[] inspectedValues) => _inspectedValues = inspectedValues;
    }
}
