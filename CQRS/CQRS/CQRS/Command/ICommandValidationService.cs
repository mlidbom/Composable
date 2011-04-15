using System.Collections.Generic;

namespace Composable.CQRS.Command
{
    public interface ICommandValidationService
    {
        IEnumerable<IValidationFailure> Validate<TCommand>(TCommand command);
    }
}