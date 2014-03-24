using AccountManagement.Domain;
using AccountManagement.TestHelpers.Scenarios;
using Castle.Windsor;

namespace AccountManagement.TestHelpers.Fixtures
{
    public class SingleAccountFixture
    {
        public Account Account { get; private set; }

        public static SingleAccountFixture Setup(IWindsorContainer container)
        {
            return new SingleAccountFixture()
                   {
                       Account = new RegisterAccountScenario(container).Execute()
                   };
        }
    }
}