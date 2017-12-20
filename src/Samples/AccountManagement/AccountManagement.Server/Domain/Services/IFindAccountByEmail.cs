namespace AccountManagement.Domain.Services
{
    interface IFindAccountByEmail
    {
        void AssertAccountDoesNotExist(Email email);
    }
}
