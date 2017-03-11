namespace Composable.Windsor.Testing
{
    ///<summary>Component that changes container wiring to enable testing.</summary>
    public interface IConfigureWiringForTests
    {
        ///<summary>Changes wiring in the container to be appropriate for testing.</summary>
        void ConfigureWiringForTesting();
    }
}