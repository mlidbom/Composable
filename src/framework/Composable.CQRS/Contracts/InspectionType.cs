namespace Composable.Contracts
{
    ///<summary> <see cref="InspectionType.Argument"/>, <see cref="InspectionType.Invariant"/> or <see cref="InspectionType.ReturnValue"/> </summary>
    public enum InspectionType
    {
        ///<summary>The inspected value is an argument to a method</summary>
        Argument,
        ///<summary>The inspected value is an invariant of the class</summary>
        Invariant,
        ///<summary>The inspected value is a return value</summary>
        ReturnValue,
        Assertion,
        ///<summary>The inspected value is part of the current state of the calling code.</summary>
        State,
        ///<summary>The inspected value is the return value of something the calling code called.</summary>
        Result
    }
}