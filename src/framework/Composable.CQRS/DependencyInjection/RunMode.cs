using System;
using NCrunch.Framework;

namespace Composable.DependencyInjection
{
    class RunMode : IRunMode
    {
        readonly bool _isTesting;
        bool IRunMode.IsTesting => _isTesting;
        public TestingMode TestingMode { get; }

        public StorageProvider TestingStorageProvider
        {
            get
            {
                var storageProviderName = NCrunchEnvironment.GetDuplicatedDimension();

                if(!Enum.TryParse(storageProviderName, out StorageProvider provider))
                {
                    throw new Exception("Failed to parse provider");
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
