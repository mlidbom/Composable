namespace AccountManagement.Domain.Shared
{
    /// <summary>
    /// If this was a real application this class would do various hashing etc of the password...
    /// </summary>
    public struct Password
    {
        private string Value { get; set; }

        public bool Matches(string password)
        {
            return Value == password;
        }

        public Password(string password) : this()
        {
            Value = password;
        }
    }
}