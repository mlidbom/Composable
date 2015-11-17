using System;
using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.ModelBuilder;

namespace Composable.CQRS.Windsor
{
    /// <summary>
    /// Add this mutator to the Container using container.Kernel.ComponentModelBuilder.AddContributor(new LifestyleRegistrationMutator());
    /// in order to allow PerWebRequest lifestyled Components to be treated as Scoped instead (this makes it work with NServiceBus and unit test)
    /// </summary>
    [Obsolete("'These extensions are now in the Composable.CQRS package. Search and replace: 'using Composable.CQRS.Windsor.Testing;' with 'using Composable.Windsor.Testing;' and 'using Composable.CQRS.Windsor;' with 'using Composable.Windsor;'", error: true)]
    public class LifestyleRegistrationMutator : IContributeComponentModelConstruction
    {
        private readonly LifestyleType _originalLifestyle;
        private readonly LifestyleType _newLifestyleType;

        public LifestyleRegistrationMutator(
            LifestyleType originalLifestyle = LifestyleType.PerWebRequest,
            LifestyleType newLifestyleType = LifestyleType.Scoped)
        {
            _originalLifestyle = originalLifestyle;
            _newLifestyleType = newLifestyleType;
        }

        public void ProcessModel(IKernel kernel,
                                 ComponentModel model)
        {
            if (model.LifestyleType == _originalLifestyle)
                model.LifestyleType = _newLifestyleType;
        }
    }
}