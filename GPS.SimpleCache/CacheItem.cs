using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GPS.SimpleCache
{
    public interface ICacheItem<TK, out TV>
    {
    }

    public sealed class CacheItem<TK, TV> : INotifyPropertyChanged, IDisposable, ICacheItem<TK, TV>
    {
        public DateTimeOffset LastUpdated = DateTimeOffset.UtcNow;
        public DateTimeOffset LastAccessed = DateTimeOffset.UtcNow;
        public TK Key { get; set; } = default(TK);
        private TV _value = default(TV);
        private static readonly object PadLock = new object();
        public event PropertyChangedEventHandler PropertyChanged;
        public bool IsDisposed { get; protected set; }

        public TV Value
        {
            get
            {
                lock (PadLock)
                {
                    LastAccessed = DateTimeOffset.UtcNow;
                    return _value;
                }
            }
            set
            {
                lock (PadLock)
                {
                    if (_value == null || !_value.Equals(value))
                    {
                        var changed = _value as INotifyPropertyChanged;
                        if (changed != null)
                        {
                            var notifyProperty = changed;
                            notifyProperty.PropertyChanged -= Notifiable_PropertyChanged;
                        }

                        _value = value;

                        var propertyChanged = _value as INotifyPropertyChanged;
                        if (propertyChanged != null)
                        {
                            var notifiable = propertyChanged;

                            notifiable.PropertyChanged += Notifiable_PropertyChanged;
                        }

                        OnPropertyChanged();
                    }
                }
            }
        }

        private void Notifiable_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Value));
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            lock (PadLock)
            {
                if (!IsDisposed)
                {
                    IsDisposed = true;

                    var value = Value as IDisposable;
                    value?.Dispose();
                }
            }
        }

        public DateTimeOffset SetLastAccessed()
        {
            return (LastAccessed = DateTimeOffset.UtcNow);
        }
    }
}
