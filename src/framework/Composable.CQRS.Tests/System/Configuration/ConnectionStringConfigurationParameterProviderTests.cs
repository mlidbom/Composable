using System.Configuration;
using Composable.System.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Configuration
{
    [TestFixture] public class ConnectionStringConfigurationParameterProviderTests
    {
        ConfigurationSqlConnectionProviderSource _providerSource;
        [SetUp] public void SetupTask() { _providerSource = new ConfigurationSqlConnectionProviderSource(new AppConfigConfigurationParameterProvider()); }

        [Test] public void ConnectionStringProvider_should_return_connection_string_specified_in_the_configuration_file() =>
            Assert.AreEqual("CSvalue1",
                            _providerSource.GetConnectionProvider("CSTest1").ConnectionString);

        [Test] public void ConnectionStringProvider_should_throw_ConfigurationErrorsException_when_name_does_not_exist() =>
            this.Invoking(_ => _providerSource.GetConnectionProvider("ErrorTest1").OpenConnection())
                .Should().Throw<ConfigurationErrorsException>();

        [Test] public void ConnectionStringProvider_exception_should_contain_ConnectionString_name() =>
            this.Invoking(_ => _providerSource.GetConnectionProvider("ErrorTest1").OpenConnection())
                .Should().Throw<ConfigurationErrorsException>()
                .And.Message.Should()
                .Contain("ErrorTest1");
    }
}
