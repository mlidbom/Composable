namespace AccountManagement
{
    public static class TestData
    {
        public static class Password
        {
            const string ValidPassword = "Pass";

            public static class Invalid
            {
                public const string Null = null;
                public static readonly string EmptyString = string.Empty;
                public static readonly string ShorterThanFourCharacters = ValidPassword.Substring(0,3);
                public static readonly  string BorderedByWhiteSpaceAtEnd = $"{ValidPassword} ";
                public static readonly  string BorderedByWhiteSpaceAtBeginning = $" {ValidPassword}";
                public static readonly  string MissingUpperCaseCharacter = ValidPassword.ToLower();
                public static readonly  string MissingLowercaseCharacter = ValidPassword.ToUpper();

                public static readonly string[] All =
                {
                    Null,
                    EmptyString,
                    ShorterThanFourCharacters,
                    BorderedByWhiteSpaceAtBeginning,
                    BorderedByWhiteSpaceAtEnd,
                    MissingUpperCaseCharacter,
                    MissingLowercaseCharacter
                };
            }

            static int _passwordCount = 1;
            internal static string CreateValidPasswordString() => $"{ValidPassword}{_passwordCount++}";
        }

        internal static class Email
        {
            static int _registeredAccounts = 1;

            public static AccountManagement.Domain.Email CreateValidEmail() => AccountManagement.Domain.Email.Parse($"test.test@test{_registeredAccounts++}.se");
        }
    }
}
