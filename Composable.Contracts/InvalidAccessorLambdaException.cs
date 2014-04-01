using System;

namespace Composable.Contracts
{
    public class InvalidAccessorLambdaException : Exception
    {
        public InvalidAccessorLambdaException() : base("The lambda passed must be of this form: '() => nameOfMemberOrParameter'.") {}
    }
}
