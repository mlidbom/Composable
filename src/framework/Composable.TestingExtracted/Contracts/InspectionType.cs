namespace Composable.Testing.Contracts
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
        Assertion
    }
}