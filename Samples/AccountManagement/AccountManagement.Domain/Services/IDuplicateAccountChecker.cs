using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain
{
    public interface IDuplicateAccountChecker
    {
        bool AccountExists(Email email);
    }
}
