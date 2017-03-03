using System;
using System.Linq.Expressions;
using Composable.System.Linq;

namespace Composable.System.Configuration
{
    ///<summary>Allows for reading configuration parameters from a source such as a configuration file</summary>
    public interface IConfigurationParameterProvider
    {
        ///<summary>Gets a string configuration value.</summary>
        string GetString(string parameterName, string valueIfMissing = null);
    }

    static class ConfigurationParameterProvider
    {
        public static bool GetBoolean(this IConfigurationParameterProvider @this, string parameterName, bool? valueIfMissing = null)
            => bool.Parse(@this.GetString(parameterName, valueIfMissing?.ToString()));
        public static bool GetBooleanFor(this IConfigurationParameterProvider @this, Expression<Func<bool>> memberPathToUseAsParameterName, bool? valueIfMissing = null)
        {
            var configurationValue = ExpressionUtil.ExtractMemberPath(memberPathToUseAsParameterName);
            return bool.Parse(@this.GetString(configurationValue, valueIfMissing?.ToString()));
        }
    }
}
