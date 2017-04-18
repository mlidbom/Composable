using System.Configuration;
using Composable.System.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Configuration
{
    [TestFixture] public class AppConfigConfigurationParameterProviderTests
    {
        AppConfigConfigurationParameterProvider _provider;
        [SetUp] public void SetupTask() { _provider = new AppConfigConfigurationParameterProvider(); }

        [Test] public void ParameterProvider_should_return_the_value_specified_in_the_configuration_file() =>
            Assert.AreEqual(ConfigurationManager.AppSettings["KeyTest1"], _provider.GetString("KeyTest1"));

        [Test] public void ParameterProvider_should_throw_ConfigurationErrorsException_when_key_does_not_exist() =>
            Assert.Throws<ConfigurationErrorsException>(() => _provider.GetString("ErrorTest1"));

        [Test] public void ParameterProvider_exception_should_contain_parameter_key() =>
            this.Invoking(_ => _provider.GetString("ErrorTest1"))
                .ShouldThrow<ConfigurationErrorsException>()
                .And.Message.Should()
                .Contain("ErrorTest1");
    }
}
