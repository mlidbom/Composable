namespace AccountManagement.Domain.Services
{
    interface IDuplicateAccountChecker
    {
        void AssertAccountDoesNotExist(Email email);
    }
}
