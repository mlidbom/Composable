using System;

namespace Composable.Testing.Contracts
{
    class AssertionException : Exception
    {
        public AssertionException(InspectionType inspectionType, string failureMessage) : base($"{inspectionType}: {failureMessage}") {}
    }
}
