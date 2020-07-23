namespace Composable.SystemCE.Configuration
{
    ///<summary>Allows for reading configuration parameters from a source such as a configuration file</summary>
    public interface IConfigurationParameterProvider
    {
        ///<summary>Gets a string configuration value.</summary>
        string GetString(string parameterName, string? valueIfMissing = null);
    }

    static class ConfigurationParameterProvider
    {
        public static bool GetBoolean(this IConfigurationParameterProvider @this, string parameterName, bool? valueIfMissing = null) => bool.Parse(@this.GetString(parameterName, valueIfMissing?.ToString()));
    }
}
