using System.Collections.Generic;

namespace Composable.CQRS.Command
{
    public interface ICommandValidator<in TCommand>
    {
        IEnumerable<IValidationFailure> Validate(TCommand command);
    }
}