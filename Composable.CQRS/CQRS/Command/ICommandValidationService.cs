using System.Collections.Generic;
using JetBrains.Annotations;

namespace Composable.CQRS.Command
{
    [UsedImplicitly]
    public interface ICommandValidationService
    {
        IEnumerable<IValidationFailure> Validate<TCommand>(TCommand command);
    }
}