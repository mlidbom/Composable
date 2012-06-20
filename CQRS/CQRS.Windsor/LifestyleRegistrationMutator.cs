using Castle.Core;
using Castle.MicroKernel;
using Castle.MicroKernel.ModelBuilder;

namespace Composable.CQRS.Windsor
{
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