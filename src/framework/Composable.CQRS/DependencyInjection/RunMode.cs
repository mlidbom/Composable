using System;
using System.IO;
using Microsoft.Extensions.Configuration;

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
                IConfiguration config = new ConfigurationBuilder()
                                       .SetBasePath(Directory.GetCurrentDirectory())
                                       .AddJsonFile("appsettings-testing.json", false, true)
                                       .Build();

                var testingSection = config.GetSection("Testing");
                var storageProviderName = testingSection["COMPOSABLE_TESTING_STORAGE_PROVIDER"];

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
