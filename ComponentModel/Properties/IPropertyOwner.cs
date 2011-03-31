#region usings

using System;
using System.ComponentModel;

#endregion

namespace Composable.System.ComponentModel.Properties
{
    public interface IPropertyOwner : INotifyPropertyChanged
    {
        void FirePropertyChanged(String propertyName);
    }
}