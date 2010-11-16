namespace Composable.System.ComponentModel.Properties
{
    public interface ISerializablePropertyOwner : IPropertyOwner
    {
        /// <summary>
        /// An implementer should do all initialization of <see cref="Property{TOwnerType,TValueType}"/> and
        /// <see cref="WrapperProperty{T}"/> fields within this method in order for events to work fully after 
        /// serialization when 
        /// </summary>
        void InitializeProperties();
    }
}