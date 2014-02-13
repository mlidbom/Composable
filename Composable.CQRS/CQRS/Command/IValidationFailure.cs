using System.Collections.Generic;

namespace Composable.CQRS.Command
{
    public interface IValidationFailure
    {
        string Message { get; }
    }

    public interface IMemberValidationFailure : IValidationFailure
    {
        IEnumerable<string> MembersInvolved { get; }
    }
}