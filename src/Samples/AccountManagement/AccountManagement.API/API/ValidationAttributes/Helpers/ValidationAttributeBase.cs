using System.ComponentModel.DataAnnotations;

namespace AccountManagement.API.ValidationAttributes.Helpers
{
    public abstract class ValidationAttributeBase : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse ReSharper incorrectly believes nullable reference types to deliver runtime guarantees.
            if(value == null)
            {
                return false; //We don't mind empty strings, but null is taboo.
            }
            return InternalIsValid(value);
        }

        protected abstract bool InternalIsValid(object value);
    }
}
