using System;

namespace Composable.Contracts
{
    ///<summary>Performs inspections on Guid instances</summary>
    public static class GuidInspector
    {
        ///<summary>Throws a <see cref="GuidIsEmptyContractViolationException"/> if any inspected value is Guid.Empty</summary>
        public static Inspected<Guid> NotEmpty(this Inspected<Guid> me)
        {
            return me.Inspect(
                inspected => inspected != Guid.Empty,
                badValue => new GuidIsEmptyContractViolationException(badValue));
        }
    }
}
