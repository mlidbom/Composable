using System;

namespace AccountManagement.UI.Web
{
    public interface IAuthenticationContext
    {
        Guid AccountId { get; }
    }
}
