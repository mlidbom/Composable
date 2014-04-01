using AccountManagement.Domain.Shared;

namespace AccountManagement.Domain.Services
{
    public interface IDuplicateAccountChecker
    {
        void AssertAccountDoesNotExist(Email email);
    }
}
