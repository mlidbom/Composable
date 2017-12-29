namespace AccountManagement.Domain.Services
{
    interface IFindAccountByEmail
    {
        bool AccountExists(Email email);
    }
}
