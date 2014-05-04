using Composable.System;

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
                public const string Empty = "";
                public const string ShorterThanFourCharacters = "a";
                public const string BorderedByWhiteSpaceAtEnd = "Urdu ";
                public const string BorderedByWhiteSpaceAtBeginning = " Urdu";
                public const string MissingUpperCaseCharacter = "urdu";
                public const string MissingLowercaseCharacter = "URDU";
            }

            private static int _passwordCount = 1;
            public static string CreateValidPassword()
            {
                return "SomeComplexPassword" + _passwordCount++;
            }
        }

        public static class Email
        {
            private static int _registeredAccounts = 1;

            public static Domain.Shared.Email CreateValidEmail()
            {
                return Domain.Shared.Email.Parse("test.test@test{0}.se".FormatWith(_registeredAccounts++));
            }
        }
    }
}
