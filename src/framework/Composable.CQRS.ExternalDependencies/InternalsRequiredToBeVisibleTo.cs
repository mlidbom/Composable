namespace Composable
{
    public static class InternalsRequiredToBeVisibleTo
    {
        public const string Assembly1 = Castle.Core.Internal.InternalsVisible.ToDynamicProxyGenAssembly2;
        public const string Assembly2 = "DynamicProxyGenAssembly2";
        public const string Assembly3 = "Composable.ReservedForFutureUse1";
        public const string Assembly4 = "Composable.ReservedForFutureUse2";
    }
}
