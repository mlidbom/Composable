using Composable.CQRS.Query.Models.Generators;

namespace AccountManagement.UI.QueryModels.Generators
{
    ///<summary>Using a custom inheritor of IQueryModelGenerator lets us keep query model generators for different systems apart in the wiring easily.</summary>
    public interface IAccountManagementQueryModelGenerator : IQueryModelGenerator
    {
         
    }
}