using System;
using System.Collections.Specialized;

namespace RxLite
{
    public class PropertyChangingEventArgs : EventArgs
    {
        public PropertyChangingEventArgs(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        public string PropertyName { get; }
    }

    public delegate void PropertyChangingEventHandler(object sender, PropertyChangingEventArgs e);

    public interface INotifyPropertyChanging
    {
        event PropertyChangingEventHandler PropertyChanging;
    }

    public interface INotifyCollectionChanging
    {
        event NotifyCollectionChangedEventHandler CollectionChanging;
    }
}