namespace AccountManagement.Domain.Services
{
    public interface IDuplicateAccountChecker
    {
        void AssertAccountDoesNotExist(Email email);
    }
}
