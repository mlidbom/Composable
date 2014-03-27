using System;

namespace AccountManagement.Web
{
    public class AuthenticationContext : IAuthenticationContext 
    {
        public Guid AccountId { get { throw new NotImplementedException(); } }//ncrunch: no coverage remove when implementing
    }
}