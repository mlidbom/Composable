using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace AccountManagement
{
    static class TestData
    {
        internal static class Passwords
        {
            internal const string ValidPassword = "Pass";

            internal static class Invalid
            {
                public const string Null = null;
                public static readonly string EmptyString = string.Empty;
                public static readonly string ShorterThanFourCharacters = ValidPassword.Substring(0, 3);
                public static readonly string BorderedByWhiteSpaceAtEnd = $"{ValidPassword} ";
                public static readonly string BorderedByWhiteSpaceAtBeginning = $" {ValidPassword}";
                public static readonly string MissingUpperCaseCharacter = ValidPassword.ToLower();
                public static readonly string MissingLowercaseCharacter = ValidPassword.ToUpper();

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

        internal static class Emails
        {
            static int _registeredAccounts = 1;

            internal static string CreateUnusedEmail() => $"test.test@test{_registeredAccounts++}.se";

            internal static IEnumerable<string> InvalidEmails => InvalidEmailsTestData.Select(@this => @this.Data);

            internal static IReadOnlyList<StringTestData> InvalidEmailsTestData =>
                new List<StringTestData>
                {
                    new StringTestData(null, "Is null null"),
                    new StringTestData(string.Empty, "Is empty"),
                    new StringTestData("test.com", "Missing @ character"),
                    new StringTestData("test@test.com ", "Missing domain"),
                    new StringTestData("te st@test.com", "Contains space"),
                    new StringTestData("test@test", "Missing domain"),
                    new StringTestData("test@test..com", "Contains \"..\""),
                    new StringTestData("test@.test.com", "Contains \"@.\""),
                    new StringTestData("test.@test.com", "Contains \".@\"")
                };


            public class StringTestData : TestData<string>
            {
                public StringTestData(string data, string description) : base(data, description)
                {
                }
            }

            public class TestData<TData> : TestCaseData
            {
                public TData Data { get; }
                public TestData(TData data, string description) : base(data)
                {
                    Data = data;
                    SetName($"{description} ==  \"{data?.ToString() ?? "NULL"}\"");
                }
            }
        }
    }
}
