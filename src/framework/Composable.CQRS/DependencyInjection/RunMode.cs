using System;
using NCrunch.Framework;

namespace Composable.DependencyInjection
{
    class RunMode : IRunMode
    {
        readonly bool _isTesting;
        bool IRunMode.IsTesting => _isTesting;
        //urgent: TestingMode should no longer be used. Just the current PersistenceLayerProvider
        public TestingMode TestingMode { get; }

        public PersistenceLayer TestingPersistenceLayer
        {
            get
            {
                //urgent:remove this hack.
                if(_isTesting && TestingMode == TestingMode.InMemory)
                {
                    return PersistenceLayer.InMemory;
                }

                var storageProviderName = NCrunchEnvironment.GetDuplicatedDimension();
                if(!Enum.TryParse(storageProviderName, out PersistenceLayer provider))
                {
                    throw new Exception("Failed to parse PersistenceLayerProvider from test environment");
                }

                return provider;
            }
        }

        public static readonly IRunMode Production = new RunMode(isTesting: false, testingMode: TestingMode.DatabasePool);

        public RunMode(bool isTesting, TestingMode testingMode)
        {
            TestingMode = testingMode;
            _isTesting = isTesting;
        }
    }
}
