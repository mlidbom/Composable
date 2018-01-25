using System;
using AccountManagement.Domain;
using AccountManagement.Domain.Passwords;

namespace AccountManagement.API
{
    interface IAccountResourceData
    {
        Guid Id { get; }
        Email Email { get; }
        HashedPassword Password { get; }
    }
}
