using System;
using Composable.System.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Configuration
{
    [TestFixture] public class AppConfigConfigurationParameterProviderTests
    {
        AppSettingsJsonConfigurationParameterProvider _provider;
        [SetUp] public void SetupTask() { _provider = new AppSettingsJsonConfigurationParameterProvider(); }

        [Test] public void ParameterProvider_should_return_the_value_specified_in_the_configuration_file() =>
            Assert.AreEqual("ValueTest1", _provider.GetString("KeyTest1"));

        [Test] public void ParameterProvider_should_throw_ConfigurationErrorsException_when_key_does_not_exist() =>
            Assert.Throws<Exception>(() => _provider.GetString("ErrorTest1"));

        [Test] public void ParameterProvider_exception_should_contain_parameter_key() =>
            this.Invoking(_ => _provider.GetString("ErrorTest1"))
                .Should().Throw<Exception>()
                .And.Message.Should()
                .Contain("ErrorTest1");
    }
}
