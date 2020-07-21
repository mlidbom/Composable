using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
                public static readonly string ShorterThanFourCharacters = ValidPassword[0..3];
                public static readonly string BorderedByWhiteSpaceAtEnd = $"{ValidPassword} ";
                public static readonly string BorderedByWhiteSpaceAtBeginning = $" {ValidPassword}";
                public static readonly string MissingUpperCaseCharacter = ValidPassword.ToLowerInvariant();
                public static readonly string MissingLowercaseCharacter = ValidPassword.ToUpperInvariant();

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

            internal static string CreateUnusedEmail() => $"test.test@test{Interlocked.Increment(ref _registeredAccounts)}.se";

            internal static IEnumerable<string> InvalidEmails => new []
                                                                 {
                                                                     null, //"Is_null"),
                                                                     string.Empty, //"Is empty"),
                                                                     "test.com", //"Missing @ character"),
                                                                     "test@test.com ", //"Ends with space"),
                                                                     "te st@test.com", //"Contains space"),
                                                                     "test@test", //"Missing domain"),
                                                                     "test@test..com", //"Contains .."),
                                                                     "test@.test.com", //"Contains @."),
                                                                     "test.@test.com", //"Contains .@")
                                                                 };

            internal static IReadOnlyList<StringTestData> InvalidEmailsTestData =>
                new List<StringTestData>
                {
                    new StringTestData(null, "Is_null"),
                    new StringTestData(string.Empty, "Is_empty"),
                    new StringTestData("test.com", "Missing_@_character"),
                    new StringTestData("test@test.com ", "Ends_with_space"),
                    new StringTestData("te st@test.com", "Contains_space"),
                    new StringTestData("test@test", "Missing_domain"),
                    new StringTestData("test@test..com", "Contains_double_dots"),
                    new StringTestData("test@.test.com", "Contains_@_followed_by_dot"),
                    new StringTestData("test.@test.com", "Contains_dot_followed_by_@")
                };


            public class StringTestData : TestData<string>
            {
                public StringTestData(string data, string description) : base(data, description)
                {
                }
            }

            public class TestData<TData> : TestCaseData
            where TData : class
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
