using System.Configuration;

namespace Composable.System.Configuration
{
    public interface IConnectionStringProvider
    {
        ConnectionStringSettings GetConnectionString(string parameterName);
    }
}