namespace Composable.Contracts
{
    ///<summary>Represents a single value that is being inspected. Keeps track of the values name and the type of inspection </summary>
    public class InspectedValue<TValue> : InspectedValue
    {
        ///<summary>The actual value being inspected</summary>
        internal TValue Value { get; private set; }

        ///<summary>Standard constructor</summary>
        internal InspectedValue(TValue value, InspectionType type, string name = "") : base(type, name)
        {
            Value = value;
        }
    }

    ///<summary>Represents a single value that is being inspected. Keeps track of the values name and the type of inspection </summary>
    public class InspectedValue
    {
        ///<summary>Standard constructor</summary>
        protected InspectedValue(InspectionType type, string name)
        {
            Type = type;
            Name = name;
        }

        ///<summary> The <see cref="InspectionType"/> of the inspection: <see cref="InspectionType.Argument"/>, <see cref="InspectionType.Invariant"/> or <see cref="InspectionType.ReturnValue"/> </summary>
        internal InspectionType Type { get; private set; }

        ///<summary>The name of an argument, a field, or a property. "ReturnValue" for return value inspections.</summary>
        internal string Name { get; private set; }
    }
}