using System;
using AccountManagement.API.ValidationAttributes.Helpers;

namespace AccountManagement.API.ValidationAttributes
{
    public class EntityIdAttribute : GuidValidationAttribute
    {
        protected override bool IsValid(Guid value) => value != Guid.Empty;
    }
}
