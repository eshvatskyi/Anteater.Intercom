using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Anteater.Intercom.Core;

public abstract class ObservableViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    protected void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(property, value))
        {
            return false;
        }

        property = value;

        RaisePropertyChanged(propertyName);

        return true;
    }
}
