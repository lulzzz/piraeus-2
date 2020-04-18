using System;
using Piraeus.GrainInterfaces;

namespace Piraeus.Grains
{
    public class ErrorObserver : IErrorObserver
    {
        public void NotifyError(string grainId, Exception error)
        {
            OnNotify?.Invoke(this, new ErrorNotificationEventArgs(grainId, error));
        }

        public event EventHandler<ErrorNotificationEventArgs> OnNotify;
    }
}