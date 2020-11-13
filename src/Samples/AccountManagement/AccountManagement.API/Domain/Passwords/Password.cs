using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using JetBrains.Annotations;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;

namespace AccountManagement.Domain.Passwords
{
    /// <summary>
    /// Note how all the business logic of a secure password is encapsulated and the instance is immutable after being created.
    /// </summary>
    public partial class Password
    {
        public byte[] Hash { get; private set; }
        public byte[] Salt { get; private set; }

#pragma warning disable IDE0051 // Remove unused private members
        [JsonConstructor]Password(byte[] hash, byte[] salt)
#pragma warning restore IDE0051 // Remove unused private members
        {
            Hash = hash;
            Salt = salt;
        }

        public Password(string password)
        {
            Policy.AssertPasswordMatchesPolicy(password); //Use a nested class to make the policy easily discoverable while keeping this class short and expressive.
            Salt = Guid.NewGuid().ToByteArray();
            Hash = PasswordHasher.HashPassword(salt: Salt, password: password);
        }

        /// <summary>
        /// Returns true if the supplied password parameter is the same string that was used to create this password.
        /// In other words if the user should succeed in logging in using that password.
        /// </summary>
        public bool IsCorrectPassword(string password) => Hash.SequenceEqual(PasswordHasher.HashPassword(Salt, password));

        public void AssertIsCorrectPassword(string attemptedPassword)
        {
            if(!IsCorrectPassword(attemptedPassword))
            {
                throw new WrongPasswordException();
            }
        }

        public static IEnumerable<ValidationResult> Validate(string password, IValidatableObject owner, Expression<Func<object>> passwordMember) => Policy.Validate(password, owner, passwordMember);
    }
}
