using System;
using System.Collections.Generic;
using System.Linq;

namespace Composable.CQRS.UI.Command
{
    public class UICommandBase : IUICommand
    {
        public UICommandBase()
        {
            Id = Guid.NewGuid();
        }
        public UICommandBase(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; set; }
        public virtual bool IsValid { get { return !ValidationErrors.Any(); } }
        public virtual IEnumerable<IUIValidationError> ValidationErrors { get { yield break; } }
    }
}