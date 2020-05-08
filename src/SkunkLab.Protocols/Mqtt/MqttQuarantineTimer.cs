using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using SkunkLab.Protocols.Mqtt.Handlers;

namespace SkunkLab.Protocols.Mqtt
{
    public class MqttQuarantineTimer : IDisposable
    {
        private readonly MqttConfig config;

        private readonly Timer timer;

        private Dictionary<ushort, RetryMessageData> container;

        private ushort currentId;

        private bool disposed;

        public MqttQuarantineTimer(MqttConfig config)
        {
            this.config = config;
            container = new Dictionary<ushort, RetryMessageData>();

            timer = new Timer(config.AckTimeout.TotalMilliseconds);
            timer.Elapsed += Timer_Elapsed;
            timer.Start();
        }

        public event EventHandler<MqttMessageEventArgs> OnRetry;

        public void Add(MqttMessage message, DirectionType direction)
        {
            if (message.QualityOfService == QualityOfServiceLevelType.AtMostOnce)
            {
                return;
            }

            if (!container.ContainsKey(message.MessageId))
            {
                try
                {
                    DateTime timeout = DateTime.UtcNow.AddMilliseconds(config.AckTimeout.TotalMilliseconds);
                    RetryMessageData amd = new RetryMessageData(message, timeout, 0, direction);
                    container.Add(message.MessageId, amd);
                }
                catch (Exception ex)
                {
                    Trace.TraceWarning("MQTT quarantine cannot add message id");
                    Trace.TraceError(ex.Message);
                }
            }
        }

        public bool ContainsKey(ushort id)
        {
            return container.ContainsKey(id);
        }

        public void Dispose()
        {
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        public ushort NewId()
        {
            currentId++;
            currentId = currentId == ushort.MaxValue ? (ushort)1 : currentId;

            while (container.ContainsKey(currentId))
            {
                currentId++;
                currentId = currentId == ushort.MaxValue ? (ushort)1 : currentId;
            }

            return currentId;
        }

        public void Remove(ushort id)
        {
            container.Remove(id);
        }

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                disposed = true;

                container.Clear();
                container = null;

                if (timer != null)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            }
        }

        private void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                List<ushort> list = new List<ushort>();

                KeyValuePair<ushort, RetryMessageData>[] kvps = container.Where(c =>
                    c.Value.NextRetryTime < DateTime.UtcNow
                    && c.Value.Direction == DirectionType.Out).ToArray();

                if (kvps.Length > 0)
                {
                    foreach (var item in kvps)
                    {
                        item.Value.Increment(config.AckTimeout);
                        container[item.Key] = item.Value;

                        if (item.Value.AttemptCount >= config.MaxRetransmit)
                        {
                            list.Add(item.Key);
                        }
                        else
                        {
                            OnRetry?.Invoke(this, new MqttMessageEventArgs(item.Value.Message));
                        }
                    }
                }

                foreach (var item in list)
                    Remove(item);
            }
            catch { }
        }
    }
}