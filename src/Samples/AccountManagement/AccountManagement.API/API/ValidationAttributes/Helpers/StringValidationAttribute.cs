namespace AccountManagement.API.ValidationAttributes.Helpers
{
    public abstract class StringValidationAttribute : ValidationAttributeBase
    {
        protected override bool InternalIsValid(object value) => IsValid((string)value);
        protected abstract bool IsValid(string value);
    }
}
