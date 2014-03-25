using System;

namespace AccountManagement.Web
{
    public interface IAuthenticationContext
    {
        Guid AccountId { get; }
    }
}