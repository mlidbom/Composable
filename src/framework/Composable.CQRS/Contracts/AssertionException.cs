using System;

namespace Composable.Contracts
{
    class AssertionException : Exception
    {
        public AssertionException(InspectionType inspectionType, string failureMessage) : base($"{inspectionType}: {failureMessage}") {}
    }
}
