namespace CQRS.Tests
{
    internal static class NCrunchExlusivelyUsesResources
    {
        public const string NServiceBus = "Global.NServiceBus";
        public const string DocumentDbMdf = "Tests.TransactionSupport.DocumentDB.mdf";
        public const string EventStoreDbMdf = "Tests.TransactionSupport.EventStore.mdf";
    }
}