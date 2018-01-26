using AccountManagement.API;

namespace AccountManagement.Scenarios
{
    public class ScenarioBase
    {
        protected AccountApi Api => AccountApi.Instance;
    }
}
