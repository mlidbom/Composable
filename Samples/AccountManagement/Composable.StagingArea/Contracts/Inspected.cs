using System;

namespace Composable.Contracts
{
    /// <summary>
    /// <para>The class that enables all the extensions that do inspections.</para>
    /// <para>Create extension methods for this class to implement inspections.</para>
    /// <code>public static Inspected&lt;Guid> NotEmpty(this Inspected&lt;Guid> me) { return me.Inspect(inspected => inspected != Guid.Empty, badValue => new GuidIsEmptyContractViolationException(badValue)); }</code>
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    public class Inspected<TValue>
    {
        private readonly InspectedValue<TValue>[] _inspectedValues;

        public Inspected<TValue> Inspect(Func<TValue, bool> isValueValid, Func<InspectedValue<TValue>, Exception> buildException = null)
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

        public Inspected(TValue value, InspectionType type, string name = "")
        {
            _inspectedValues = new[] {new InspectedValue<TValue>(value, type, name)};
        }

        public Inspected(params InspectedValue<TValue>[] inspectedValues)
        {
            _inspectedValues = inspectedValues;
        }
    }
}
