using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain.Services
{
    public class DuplicateAccountChecker : IDuplicateAccountChecker
    {
        public DuplicateAccountChecker()
        {
            
        }
        public bool AccountExists(Email email)
        {
            return false;//Todo: real implementation please!
        }
    }
}