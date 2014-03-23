using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain
{
    public interface IDuplicateAccountChecker
    {
        bool AccountExists(Email email);
    }

    public class DuplicateAccountChecker : IDuplicateAccountChecker
    {
        public bool AccountExists(Email email)
        {
            return false;//Todo: real implementation please!
        }
    }
}
