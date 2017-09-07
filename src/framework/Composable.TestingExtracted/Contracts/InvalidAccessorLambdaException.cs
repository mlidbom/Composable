using System;

namespace Composable.Testing.Contracts
{
    ///<summary>Exception thrown when an unsupported lambda expression is used.</summary>
    class InvalidAccessorLambdaException : Exception
    {
        ///<summary>Standard constructor</summary>
        public InvalidAccessorLambdaException() : base("The lambda passed must be of this form: '() => nameOfMemberOrParameter'.") {}
    }
}
