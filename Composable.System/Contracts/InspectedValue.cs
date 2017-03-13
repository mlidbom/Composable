namespace Composable.Contracts
{
    public interface IInspectedValue<TValue> : IInspectedValue
    {
        TValue Value { get; }
    }
    ///<summary>Represents a single value that is being inspected. Keeps track of the values name and the type of inspection </summary>
    class InspectedValue<TValue> : InspectedValue, IInspectedValue<TValue>
    {
        ///<summary>The actual value being inspected</summary>
        public TValue Value { get; private set; }

        ///<summary>Standard constructor</summary>
        internal InspectedValue(TValue value, InspectionType type, string name = "") : base(type, name) => Value = value;
    }


    public interface IInspectedValue
    {
        ///<summary> The <see cref="InspectionType"/> of the inspection: <see cref="InspectionType.Argument"/>, <see cref="InspectionType.Invariant"/> or <see cref="InspectionType.ReturnValue"/> </summary>
        InspectionType Type { get; }

        ///<summary>The name of an argument, a field, or a property. "ReturnValue" for return value inspections.</summary>
        string Name { get;  }
    }
    ///<summary>Represents a single value that is being inspected. Keeps track of the values name and the type of inspection </summary>
    class InspectedValue : IInspectedValue
    {
        ///<summary>Standard constructor</summary>
        protected InspectedValue(InspectionType type, string name)
        {
            Type = type;
            Name = name;
        }

        ///<summary> The <see cref="InspectionType"/> of the inspection: <see cref="InspectionType.Argument"/>, <see cref="InspectionType.Invariant"/> or <see cref="InspectionType.ReturnValue"/> </summary>
        public InspectionType Type { get; private set; }

        ///<summary>The name of an argument, a field, or a property. "ReturnValue" for return value inspections.</summary>
        public string Name { get; private set; }
    }
}