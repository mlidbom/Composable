using System.Configuration;
using Composable.System.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.System.Configuration
{
    [TestFixture]
    public class AppConfigConfigurationParameterProviderTests
    {
        [Test]
        public void ParameterProvider_should_return_the_value_specified_in_the_configuration_file()
        {
            //Arrange
            var provider = new AppConfigConfigurationParameterProvider();

            //OldObsoletedAssert
            Assert.AreEqual(ConfigurationManager.AppSettings["KeyTest1"], provider.GetString("KeyTest1"));
        }

        [Test]
        public void ParameterProvider_should_throw_ConfigurationErrorsException_when_key_does_not_exist()
        {
            //Arrange
            var provider = new AppConfigConfigurationParameterProvider();

            //OldObsoletedAssert
            Assert.Throws<ConfigurationErrorsException>(() => provider.GetString("ErrorTest1"));
        }

        [Test]
        public void ParameterProvider_exception_should_contain_parameter_key()
        {
            //Arrange
            var parameterKey = "ErrorTest1";
            var provider = new AppConfigConfigurationParameterProvider();

            //OldObsoletedAssert
            var exc=Assert.Throws<ConfigurationErrorsException>(() => provider.GetString(parameterKey));
            exc.Message.Should().Contain(parameterKey);
        }
    }
}
