using System;
using AccountManagement.Domain;

namespace AccountManagement.API
{
    interface IAccountResourceData
    {
        Guid Id { get; }
        Email Email { get; }
        Password Password { get; }
    }
}
