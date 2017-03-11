using Composable.CQRS.CQRS.Query.Models.Generators;

namespace AccountManagement.UI.QueryModels.EventStoreGenerated
{
    ///<summary>Using a custom inheritor of IQueryModelGenerator lets us keep query model generators for different systems apart in the wiring easily.</summary>
    interface IAccountManagementQueryModelGenerator : IQueryModelGenerator {}
}
