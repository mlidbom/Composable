using System;
using System.ComponentModel;

namespace Composable.System.ComponentModel.Properties
{
    public interface IPropertyOwner : INotifyPropertyChanged
    {
        void FirePropertyChanged(String propertyName);
    }
}