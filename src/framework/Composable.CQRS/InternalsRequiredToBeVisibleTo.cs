namespace Composable
{
    public static class InternalsRequiredToBeVisibleTo
    {
        public const string Assembly1 = "DynamicProxyGenAssembly2";
        public const string Assembly2 = "Composable.ReservedForFutureUse1";
        public const string Assembly3 = "Composable.ReservedForFutureUse2";
        public const string Assembly4 = "Composable.ReservedForFutureUse3";
        //public const string Assembly4 = Castle.Core.Internal.InternalsVisible.ToDynamicProxyGenAssembly2;
    }
}
