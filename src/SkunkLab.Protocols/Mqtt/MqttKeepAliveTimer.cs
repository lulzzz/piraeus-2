using System;
using System.Threading;

namespace SkunkLab.Protocols.Mqtt
{
    public class MqttKeepAliveTimer : IDisposable
    {
        private readonly int period;

        private Timer timer;

        public MqttKeepAliveTimer(int periodMilliseconds)
        {
            period = periodMilliseconds;
        }

        public event EventHandler OnExpired;

        public void Callback(object state)
        {
            if (OnExpired != null)
            {
                OnExpired(this, new EventArgs());
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Reset()
        {
            timer.Dispose();
            timer = new Timer(Callback, null, period, period);
        }

        public void Start()
        {
            timer = new Timer(Callback, null, period, period);
        }

        public void Stop()
        {
            timer.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                timer.Dispose();
            }
        }
    }
}