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
        }
    }
}
