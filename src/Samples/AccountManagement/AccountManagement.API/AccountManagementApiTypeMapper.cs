using Composable.Refactoring.Naming;

namespace AccountManagement
{
    public class AccountManagementApiTypeMapper
    {
        public static void MapTypes(ITypeMappingRegistar typeMapper)
        {
            typeMapper
               .Map<API.AccountResource.Command.ChangeEmail>("f38f0473-e0cc-4ef7-9ff6-4e99da03a39e")
               .Map<API.AccountResource.Command.Register>("1C8342B3-1302-40D1-BD54-1333A47F756F")
               .Map<API.AccountResource.Command.ChangePassword>("077F075B-64A3-4E02-B435-F04B19F6C98D")
               .Map<API.AccountResource.Command.LogIn>("90689406-de88-43da-be17-0fb93692f6ad")
               .Map<API.AccountResource>("ad443c81-a759-49af-a839-2befba89d3d4")
               .Map<API.AccountResource.Command.LogIn.LoginAttemptResult>("0ff5734d-6ee5-40f9-a022-b38cd523d6e5")
               .Map<API.AccountResource.Command.Register.RegistrationAttemptResult>("5072db5b-14f1-42d9-add9-b1dd336eee8f");
        }
    }
}
