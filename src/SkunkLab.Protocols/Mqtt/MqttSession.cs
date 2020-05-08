using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using SkunkLab.Protocols.Mqtt.Handlers;
using SkunkLab.Security.Tokens;

namespace SkunkLab.Protocols.Mqtt
{
    public delegate void ConnectionHandler(object sender, MqttConnectionArgs args);

    public delegate void EventHandler<MqttMessageEventArgs>(object sender, MqttMessageEventArgs args);

    public delegate List<string> SubscriptionHandler(object sender, MqttMessageEventArgs args);

    public class MqttSession : IDisposable
    {
        private readonly PublishContainer pubContainer;

        private readonly MqttQuarantineTimer quarantine;

        private string bootstrapToken;

        private SecurityTokenType bootstrapTokenType;

        private bool disposed;

        private DateTime keepaliveExpiry;

        private double keepaliveSeconds;

        private Timer keepaliveTimer;

        private Dictionary<string, QualityOfServiceLevelType> qosLevels;

        public MqttSession(MqttConfig config)
        {
            Config = config;
            KeepAliveSeconds = config.KeepAliveSeconds;
            pubContainer = new PublishContainer(config);

            qosLevels = new Dictionary<string, QualityOfServiceLevelType>();
            quarantine = new MqttQuarantineTimer(config);
            quarantine.OnRetry += Quarantine_OnRetry;
        }

        public event ConnectionHandler OnConnect;

        public event EventHandler<MqttMessageEventArgs> OnDisconnect;

        public event EventHandler<MqttMessageEventArgs> OnKeepAlive;

        public event EventHandler<MqttMessageEventArgs> OnPublish;

        public event EventHandler<MqttMessageEventArgs> OnRetry;

        public event SubscriptionHandler OnSubscribe;

        public event EventHandler<MqttMessageEventArgs> OnUnsubscribe;

        public MqttConfig Config
        {
            get; set;
        }

        public ConnectAckCode ConnectResult
        {
            get; internal set;
        }

        public bool HasBootstrapToken
        {
            get; internal set;
        }

        public string Identity
        {
            get; set;
        }

        public List<KeyValuePair<string, string>> Indexes
        {
            get; set;
        }

        public bool IsAuthenticated
        {
            get; set;
        }

        public bool IsConnected
        {
            get; internal set;
        }

        public bool Authenticate()
        {
            if (!HasBootstrapToken)
            {
                return false;
            }

            IsAuthenticated = Config.Authenticator.Authenticate(bootstrapTokenType, bootstrapToken);
            return IsAuthenticated;
        }

        public bool Authenticate(string tokenType, string token)
        {
            SecurityTokenType tt = (SecurityTokenType)Enum.Parse(typeof(SecurityTokenType), tokenType, true);
            bootstrapTokenType = tt;
            bootstrapToken = token;
            HasBootstrapToken = true;

            IsAuthenticated = Config.Authenticator.Authenticate(tt, token);
            return IsAuthenticated;
        }

        public bool Authenticate(byte[] message)
        {
            ConnectMessage msg = (ConnectMessage)MqttMessage.DecodeMessage(message);
            return Authenticate(msg.Username, msg.Password);
        }

        public bool Authenticate(ConnectMessage msg)
        {
            return Authenticate(msg.Username, msg.Password);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public ushort NewId()
        {
            return quarantine.NewId();
        }

        public async Task<MqttMessage> ReceiveAsync(MqttMessage message)
        {
            MqttMessageHandler handler = MqttMessageHandler.Create(this, message);
            return await handler.ProcessAsync();
        }

        #region Retry Signal

        private void Quarantine_OnRetry(object sender, MqttMessageEventArgs args)
        {
            MqttMessage msg = args.Message;
            msg.Dup = true;
            OnRetry?.Invoke(this, new MqttMessageEventArgs(msg));
        }

        #endregion Retry Signal

        protected virtual void Dispose(bool dispose)
        {
            if (dispose & !disposed)
            {
                quarantine.Dispose();
                pubContainer.Dispose();
                qosLevels.Clear();
                qosLevels = null;

                keepaliveTimer?.Dispose();
            }

            disposed = true;
        }

        #region QoS Management

        public void AddQosLevel(string topic, QualityOfServiceLevelType qos)
        {
            if (!qosLevels.ContainsKey(topic))
            {
                qosLevels.Add(topic, qos);
            }
        }

        public QualityOfServiceLevelType? GetQoS(string topic)
        {
            if (qosLevels.ContainsKey(topic))
            {
                return qosLevels[topic];
            }

            return null;
        }

        #endregion QoS Management

        #region internal function calls from handlers

        internal void Connect(ConnectAckCode code)
        {
            ConnectResult = code;
            OnConnect?.Invoke(this, new MqttConnectionArgs(code));
        }

        internal void Disconnect(MqttMessage message)
        {
            OnDisconnect?.Invoke(this, new MqttMessageEventArgs(message));
        }

        internal void Publish(MqttMessage message, bool force = false)
        {
            if (message.QualityOfService != QualityOfServiceLevelType.ExactlyOnce
                || message.QualityOfService == QualityOfServiceLevelType.ExactlyOnce && force)
            {
                OnPublish?.Invoke(this, new MqttMessageEventArgs(message));
            }
        }

        internal List<string> Subscribe(MqttMessage message)
        {
            return OnSubscribe?.Invoke(this, new MqttMessageEventArgs(message));
        }

        internal void Unsubscribe(MqttMessage message)
        {
            OnUnsubscribe?.Invoke(this, new MqttMessageEventArgs(message));
        }

        #endregion internal function calls from handlers

        #region QoS 2 functions

        internal MqttMessage GetHeldMessage(ushort id)
        {
            if (pubContainer.ContainsKey(id))
            {
                return pubContainer[id];
            }

            return null;
        }

        internal void HoldMessage(MqttMessage message)
        {
            if (!pubContainer.ContainsKey(message.MessageId))
            {
                pubContainer.Add(message.MessageId, message);
            }
        }

        internal void ReleaseMessage(ushort id)
        {
            pubContainer.Remove(id);
        }

        #endregion QoS 2 functions

        #region keep alive

        internal double KeepAliveSeconds
        {
            get => keepaliveSeconds;
            set
            {
                keepaliveSeconds = value;

                if (keepaliveTimer == null)
                {
                    keepaliveTimer = new Timer(Convert.ToDouble(value * 1000));
                    keepaliveTimer.Elapsed += KeepaliveTimer_Elapsed;
                    keepaliveTimer.Start();
                }
            }
        }

        internal void IncrementKeepAlive()
        {
            keepaliveExpiry = DateTime.UtcNow.AddSeconds(Convert.ToDouble(keepaliveSeconds));
        }

        internal void StopKeepAlive()
        {
            keepaliveTimer.Stop();
            keepaliveTimer = null;
        }

        private void KeepaliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (keepaliveExpiry < DateTime.Now)
            {
                OnKeepAlive?.Invoke(this, new MqttMessageEventArgs(new PingRequestMessage()));
            }
        }

        #endregion keep alive

        #region ID Quarantine

        public bool IsQuarantined(ushort messageId)
        {
            return quarantine.ContainsKey(messageId);
        }

        public void Quarantine(MqttMessage message, DirectionType direction)
        {
            quarantine.Add(message, direction);
        }

        public void Unquarantine(ushort messageId)
        {
            quarantine.Remove(messageId);
        }

        #endregion ID Quarantine
    }
}