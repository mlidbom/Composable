using System.Configuration;
using Composable.System.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Configuration
{
    [TestFixture] public class ConnectionStringConfigurationParameterProviderTests
    {
        AppConfigConnectionStringProvider _provider;
        [SetUp] public void SetupTask() { _provider = new AppConfigConnectionStringProvider(); }

        [Test] public void ConnectionStringProvider_should_return_connection_string_specified_in_the_configuration_file() =>
            Assert.AreEqual(ConfigurationManager.ConnectionStrings["CSTest1"]
                                                .ConnectionString,
                            _provider.GetConnectionString("CSTest1"));

        [Test] public void ConnectionStringProvider_shuld_throw_ConfigurationErrorsException_when_name_does_not_exist() =>
            this.Invoking(_ => _provider.GetConnectionString("ErrorTest1"))
                .ShouldThrow<ConfigurationErrorsException>();

        [Test] public void ConnectionStringProvider_exception_should_contain_ConnectionString_name() =>
            this.Invoking(_ => _provider.GetConnectionString("ErrorTest1"))
                .ShouldThrow<ConfigurationErrorsException>()
                .And.Message.Should()
                .Contain("ErrorTest1");
    }
}
