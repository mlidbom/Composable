using System;

namespace Void.ComponentModel.Properties
{
    public interface IProperty
    {
        event Action<string> PropertyChanged;
        void Initialize(IPropertyOwner owner);
    }
}