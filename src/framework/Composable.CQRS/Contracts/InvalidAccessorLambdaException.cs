using System;

namespace Composable.Contracts
{
    ///<summary>Exception thrown when an unsupported lambda expression is used.</summary>
    public class InvalidAccessorLambdaException : Exception
    {
        ///<summary>Standard constructor</summary>
        public InvalidAccessorLambdaException() : base("The lambda passed must be of this form: '() => nameOfMemberOrParameter'.") {}
    }
}
