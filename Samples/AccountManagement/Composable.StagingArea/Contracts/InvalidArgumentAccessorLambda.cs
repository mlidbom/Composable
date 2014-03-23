using System;

namespace Composable.Contracts
{
    public class InvalidArgumentAccessorLambda : Exception
    {
        public InvalidArgumentAccessorLambda() : base("The lambda passed must be exactly of this form: '() => parameterName' where parameterName really is the name of a parameter.") { }
    }
}