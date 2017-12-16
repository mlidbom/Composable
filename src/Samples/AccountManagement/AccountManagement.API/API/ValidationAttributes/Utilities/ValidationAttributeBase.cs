using System.ComponentModel.DataAnnotations;

namespace AccountManagement.API.ValidationAttributes.Utilities
{
    public abstract class ValidationAttributeBase : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if(value == null)
            {
                return true; //Validating whether something is required is the job of the Required attribute so other attributes always consider null to be valid.
            }
            return InternalIsValid(value);
        }

        protected abstract bool InternalIsValid(object value);
    }
}
