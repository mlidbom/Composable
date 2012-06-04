using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Registration.Lifestyle;

namespace Composable.CQRS.Windsor
{
    public static class WindsorLifestyleRegistrationExtensions
    {
        private enum RuntimeType
        {
            Unknown,
            RealProject,
            TestProject
        }

        private static RuntimeType _projectType = RuntimeType.Unknown;

        public static void ResetCallOnlyFromTests()
        {
            _projectType = RuntimeType.Unknown;
        }

        public static void InitAsTestProjectOnlyUseFromTests()
        {
            if(_projectType == RuntimeType.RealProject)
            {
                throw new Exception("Attempt to change project type from RealProject to TestProject");
            }
            _projectType = RuntimeType.TestProject;
        }

        public static void InitAsRealProject()
        {
            if (_projectType == RuntimeType.TestProject)
            {
                throw new Exception("Attempt to change project type from TestProject to RealProject");
            }
            _projectType = RuntimeType.RealProject;
        }

        public static ComponentRegistration<S> PerNserviceBusMessage<S>(this LifestyleGroup<S> lifetLifestyleGroup) where S : class
        {
            if(_projectType == RuntimeType.Unknown)
            {
                throw new Exception("You must initialize WindsorLifestyleRegistrationExtensions first. Call one of the InitAs... methods");
            }
            return _projectType == RuntimeType.RealProject ? lifetLifestyleGroup.Scoped() : lifetLifestyleGroup.Singleton;
        }
    }
}