using System;
using System.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace Composable.Tests.TestingNunit3Attribute
{
    public class RunOnlyForWebsitesAttribute : IgnoreAttribute
    {
        public RunOnlyForWebsitesAttribute(Website websites) : base("Excluded because the test belongs to the wrong website.")
        {
            Until = !websites.HasFlag(TestEnvironment.Website)
                        ? DateTime.MaxValue.ToString("O")
                        : DateTime.MinValue.ToString("O");
        }
    }

    public class DontRunForWebsitesAttribute : IgnoreAttribute
    {
        public DontRunForWebsitesAttribute(Website websites) : base("Excluded because the test belongs to the wrong website.")
        {
            Until = websites.HasFlag(TestEnvironment.Website)
                        ? DateTime.MaxValue.ToString("O")
                        : DateTime.MinValue.ToString("O");
        }
    }

    [Flags]
    public enum Website
    {
        ManpowerSe = 2,
        ManpowerFi = 4,
        ManpowerDk = 8
    }

    [TestFixture]
    public class TestSiteSpecificAttributes
    {
        [Test, RunOnlyForWebsites(Website.ManpowerFi)]
        public void OnlyExecutedForManpowerFi() => TestEnvironment.Website.Should().Be(Website.ManpowerFi);

        [Test, DontRunForWebsites(Website.ManpowerFi)]
        public void NotExecutedForManpowerFi() => TestEnvironment.Website.Should().NotBe(Website.ManpowerFi);

        [Test, RunOnlyForWebsites(Website.ManpowerSe | Website.ManpowerDk)]
        public void OnlyExecutedForManpowerSeorManpoewrDk() => (TestEnvironment.Website == Website.ManpowerSe || TestEnvironment.Website == Website.ManpowerDk).Should().Be(true);
    }

    public static class TestEnvironment
    {
        public static Website Website => (Website)Enum.Parse(typeof(Website), ConfigurationManager.AppSettings.Get("WebSite"));
    }
}