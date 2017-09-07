using JetBrains.Annotations;

namespace Composable.Testing.Contracts
{
    static class ContractOptimized
    {
        public static Inspected<object> Argument(object p1,
                                                 [InvokerParameterName] string n1,
                                                 object p2,
                                                 [InvokerParameterName] string n2) =>
            new Inspected<object>(new InspectedValue<object>(p1, InspectionType.Argument, n1),
                                  new InspectedValue<object>(p2, InspectionType.Argument, n2));
    }
}
