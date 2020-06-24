using System;
using NCrunch.Framework;

namespace Composable.DependencyInjection
{
    class RunMode : IRunMode
    {
        readonly bool _isTesting;
        bool IRunMode.IsTesting => _isTesting;

        public PersistenceLayer TestingPersistenceLayer
        {
            get
            {
                var storageProviderName = NCrunchEnvironment.GetDuplicatedDimension();
                if(!Enum.TryParse(storageProviderName, out PersistenceLayer provider))
                {
                    throw new Exception("Failed to parse PersistenceLayerProvider from test environment");
                }

                return provider;
            }
        }

        public static readonly IRunMode Production = new RunMode(isTesting: false);

        public RunMode(bool isTesting)
        {
            _isTesting = isTesting;
        }
    }
}
