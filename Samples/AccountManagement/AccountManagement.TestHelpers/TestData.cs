namespace AccountManagement.TestHelpers
{
    public static class TestData
    {
        public static class Password
        {
            public static class Invalid
            {
                public static string[] All =
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
                public const string BorderedByWhiteSpaceAtBeginning = " Urdu";
                public const string MissingUpperCaseCharacter = "urdu";
                public const string MissingLowercaseCharacter = "URDU";
            }

            static int _passwordCount = 1;
            public static string CreateValidPasswordString()
            {
                return "SomeComplexPassword" + _passwordCount++;
            }

            public static Domain.Shared.Password CreateValidPassword()
            {
                return new Domain.Shared.Password(CreateValidPasswordString());
            }
        }

        public static class Email
        {
            static int _registeredAccounts = 1;

            public static Domain.Shared.Email CreateValidEmail()
            {
                return Domain.Shared.Email.Parse($"test.test@test{_registeredAccounts++}.se");
            }
        }
    }
}
