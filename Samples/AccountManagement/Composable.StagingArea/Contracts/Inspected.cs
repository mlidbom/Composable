using System;

namespace Composable.Contracts
{
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

    public enum InspectionType
    {
        Argument,
        Invariant,
        ReturnValue
    }

    public class InspectedValue
    {
        protected InspectedValue(InspectionType type, string name)
        {
            Type = type;
            Name = name;
        }

        public InspectionType Type { get; private set; }
        public string Name { get; private set; }
    }

    public class InspectedValue<TValue> : InspectedValue
    {
        public TValue Value { get; private set; }

        public InspectedValue(TValue value, InspectionType type, string name = "") : base(type, name)
        {
            Value = value;
        }
    }
}
