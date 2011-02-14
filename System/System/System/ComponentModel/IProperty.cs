using System;

namespace Composable.System.ComponentModel.Properties
{
    public interface IProperty
    {
        event Action<string> PropertyChanged;
        void Initialize(IPropertyOwner owner);
    }
}