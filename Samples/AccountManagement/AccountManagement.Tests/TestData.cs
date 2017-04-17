namespace AccountManagement.Tests
{
    public static class TestData
    {
        public static class Password
        {
            public static class Invalid
            {
                public static readonly string[] All =
                {
                    Null,
                    ShorterThanFourCharacters,
                    BorderedByWhiteSpaceAtBeginning,
                    BorderedByWhiteSpaceAtEnd,
                    MissingUpperCaseCharacter,
                    MissingLowercaseCharacter
                };

                public const string Null = null;
                public const string ShorterThanFourCharacters = "a";
                public const string BorderedByWhiteSpaceAtEnd = "Urdu ";
                const string BorderedByWhiteSpaceAtBeginning = " Urdu";
                public const string MissingUpperCaseCharacter = "urdu";
                public const string MissingLowercaseCharacter = "URDU";
            }

            static int _passwordCount = 1;
            internal static string CreateValidPasswordString() => "SomeComplexPassword" + _passwordCount++;

            public static AccountManagement.Domain.Password CreateValidPassword() => new AccountManagement.Domain.Password(CreateValidPasswordString());
        }

        internal static class Email
        {
            static int _registeredAccounts = 1;

            public static AccountManagement.Domain.Email CreateValidEmail() => AccountManagement.Domain.Email.Parse($"test.test@test{_registeredAccounts++}.se");
        }
    }
}
