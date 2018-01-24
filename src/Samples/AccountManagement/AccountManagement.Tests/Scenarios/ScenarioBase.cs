using AccountManagement.API;

namespace AccountManagement.Tests.Scenarios
{
    public class ScenarioBase
    {
        protected AccountApi Api => AccountApi.Instance;
    }
}
