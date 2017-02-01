using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.ModelBuilder;

namespace Composable.Windsor
{
    /// <summary>
    /// Add this mutator to the Container using container.Kernel.ComponentModelBuilder.AddContributor(new LifestyleRegistrationMutator());
    /// in order to allow PerWebRequest lifestyled Components to be treated as Scoped instead (this makes it work with NServiceBus and unit test)
    /// </summary>
    public class LifestyleRegistrationMutator : IContributeComponentModelConstruction
    {
        readonly LifestyleType _originalLifestyle;
        readonly LifestyleType _newLifestyleType;

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