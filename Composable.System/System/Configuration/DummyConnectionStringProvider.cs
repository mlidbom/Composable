using System.Configuration;

namespace Composable.System.Configuration
{
    ///<summary>Always returns a default connection string: "Composable.System.Configuration.DummyConnectionStringProvider.DummyConnectionString"</summary>
    class DummyConnectionStringProvider : IConnectionStringProvider
    {
        ///<summary>Always returns a default connection string: "Composable.System.Configuration.DummyConnectionStringProvider.DummyConnectionString"</summary>
        public ConnectionStringSettings GetConnectionString(string parameterName) => new ConnectionStringSettings(parameterName, "Composable.System.Configuration.DummyConnectionStringProvider.DummyConnectionString");
    }
}