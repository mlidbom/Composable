namespace Composable.System.Configuration
{
    public interface IConfigurationParameterProvider
    {
        string GetString(string parameterName);
    }
}