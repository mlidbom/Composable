namespace Composable.Contracts
{
    public class ContractOptimized
    {
        public static Inspected<object> Argument(object argumentValue1, string argumentName1) =>
            new Inspected<object>(new InspectedValue<object>(argumentValue1, InspectionType.Argument, argumentName1));

        public static Inspected<object> Argument(object p1, string n1, object p2, string n2)
        {
            return new Inspected<object>(new InspectedValue<object>(p1, InspectionType.Argument, n1),
                                         new InspectedValue<object>(p2, InspectionType.Argument, n2));
        }

        public static Inspected<object> Argument(object p1, string n1, object p2, string n2, object p3, string n3)
        {
            return new Inspected<object>(new InspectedValue<object>(p1, InspectionType.Argument, n1),
                                         new InspectedValue<object>(p2, InspectionType.Argument, n2),
                                         new InspectedValue<object>(p3, InspectionType.Argument, n3));
        }
    }
}
