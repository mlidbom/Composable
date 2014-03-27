using System;
using System.ComponentModel.DataAnnotations;

namespace AccountManagement.UI.Commands.ValidationAttributes
{
    public class EntityIdAttribute : ValidationAttribute
    {
        override public bool IsValid(object value)
        {
            if(value == null)
            {
                return true; //We validate that values are correct emails. Not that they are present. That is for the Required attribute.
            }
            return (Guid)value != Guid.Empty;
        }
    }
}
