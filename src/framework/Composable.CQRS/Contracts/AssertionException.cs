using System;

namespace Composable.Contracts
{
    public class AssertionException : Exception
    {
        public AssertionException(InspectionType inspectionType, string failureMessage) : base($"{inspectionType}: {failureMessage}") {}
    }
}
