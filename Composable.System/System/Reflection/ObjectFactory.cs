using System;
using System.Reflection;

namespace Composable.System.Reflection
{
    ///<summary>Constructs instances of classes</summary>
    public static class ObjectFactory<TEntity>
    {
        ///<summary>Creates an instance of TEntity using a constructor that takes one argument of type TArgument1</summary>
        public static TEntity CreateInstance<TArgument1>(TArgument1 argument1)
        {
            try
            {
                return (TEntity)Activator.CreateInstance(
                    type: typeof(TEntity),
                    bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: new object[] {argument1},
                    culture: null);
            }
            catch(MissingMethodException exception)
            {
                throw new Exception(
                    $"Type: {typeof(TEntity).FullName} must have a constructor taking a single argument of type: {typeof(TArgument1).FullName}",
                    exception);
            }
        }
    }
}
