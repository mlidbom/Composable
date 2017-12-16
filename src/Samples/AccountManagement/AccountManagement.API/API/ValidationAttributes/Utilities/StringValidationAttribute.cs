namespace AccountManagement.API.ValidationAttributes.Utilities
{
    public abstract class StringValidationAttribute : ValidationAttributeBase
    {
        protected override bool InternalIsValid(object value) => IsValid((string)value);
        protected abstract bool IsValid(string value);
    }
}
