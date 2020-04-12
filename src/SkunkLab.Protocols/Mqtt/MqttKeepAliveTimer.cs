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
            this.period = periodMilliseconds;
        }

        public event EventHandler OnExpired;

        public void Callback(object state)
        {
            if (this.OnExpired != null)
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
            this.timer.Dispose();
            this.timer = new Timer(new TimerCallback(Callback), null, this.period, period);
        }

        public void Start()
        {
            this.timer = new Timer(new TimerCallback(Callback), null, this.period, this.period);
        }

        public void Stop()
        {
            this.timer.Dispose();
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