using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain
{
    public interface IDuplicateAccountChecker
    {
        void AssertAccountDoesNotExist(Email email);
    }
}
