using System.Collections.Generic;

namespace Composable.CQRS.Command
{
    public class MemberValidationFailure : ValidationFailure, IMemberValidationFailure
    {
        public IEnumerable<string> MembersInvolved { get; set; }
    }
}