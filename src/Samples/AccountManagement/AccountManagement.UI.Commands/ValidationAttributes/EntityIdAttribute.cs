using System;
using System.ComponentModel.DataAnnotations;

namespace AccountManagement.UI.Commands.ValidationAttributes
{
    public class EntityIdAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if(value == null)
            {
                return true; //We validate that values are correct entity ids. Not that they are present. That is the job of the Required attribute.
            }
            return (Guid)value != Guid.Empty;
        }
    }
}
