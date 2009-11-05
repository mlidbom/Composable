using System;
using System.ComponentModel;

namespace Void.ComponentModel.Properties
{
    public interface IPropertyOwner : INotifyPropertyChanged
    {
        void FirePropertyChanged(String propertyName);
    }
}