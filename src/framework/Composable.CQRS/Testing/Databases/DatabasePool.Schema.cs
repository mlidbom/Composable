namespace Composable.Testing.Databases
{
    static class DatabaseExtensions
    {
        internal static string Name(this DatabasePool.Database @this) => $"{DatabasePool.PoolDatabaseNamePrefix}{@this.Id:0000}";
    }
}