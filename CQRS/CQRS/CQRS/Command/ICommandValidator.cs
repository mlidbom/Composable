using System.Collections.Generic;
using JetBrains.Annotations;

namespace Composable.CQRS.Command
{
    [UsedImplicitly]
    public interface ICommandValidator<in TCommand>
    {
        IEnumerable<IValidationFailure> Validate(TCommand command);
    }
}