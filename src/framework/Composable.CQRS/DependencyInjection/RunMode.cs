namespace Composable.DependencyInjection
{
    class RunMode : IRunMode
    {
        readonly bool _isTesting;
        bool IRunMode.IsTesting => _isTesting;
        public TestingMode TestingMode { get; }

        public static readonly IRunMode Production = new RunMode(isTesting: false, testingMode: TestingMode.DatabasePool);

        public RunMode(bool isTesting, TestingMode testingMode)
        {
            TestingMode = testingMode;
            _isTesting = isTesting;
        }
    }
}
