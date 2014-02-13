#region usings

using System;

#endregion

namespace Composable.System.ComponentModel.Properties
{
    public interface IProperty
    {
        event Action<string> PropertyChanged;
        void Initialize(IPropertyOwner owner);
    }
}