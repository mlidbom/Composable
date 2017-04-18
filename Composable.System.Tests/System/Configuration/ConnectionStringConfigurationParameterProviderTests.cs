using System.Configuration;
using Composable.System.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Configuration
{
    [TestFixture]
    public class ConnectionStringConfigurationParameterProviderTests
    {
        [Test]
        public void ConnectionStringProvider_should_return_connection_string_specified_in_the_configuration_file()
        {
            //Arrange
            var provider = new AppConfigConnectionStringProvider();

            //OldObsoletedAssert
            Assert.AreEqual(ConfigurationManager.ConnectionStrings["CSTest1"], provider.GetConnectionString("CSTest1"));
        }

        [Test]
        public void ConnectionStringProvider_shuld_throw_ConfigurationErrorsException_when_name_does_not_exist()
        {
            //Arrange
            var provider = new AppConfigConnectionStringProvider();

            //OldObsoletedAssert
            Assert.Throws<ConfigurationErrorsException>(() => provider.GetConnectionString("ErrorTest1"));
        }


        [Test]
        public void ConnectionStringProvider_exception_should_contain_ConnectionString_name()
        {
            //Arrange
            var parameterKey = "ErrorTest1";
            var provider = new AppConfigConnectionStringProvider();

            //OldObsoletedAssert
            var exc = Assert.Throws<ConfigurationErrorsException>(() => provider.GetConnectionString(parameterKey));
            exc.Message.Should().Contain(parameterKey);
        }
    }
}
