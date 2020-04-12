﻿using SkunkLab.Protocols.Mqtt.Handlers;
using SkunkLab.Security.Tokens;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace SkunkLab.Protocols.Mqtt
{
    public delegate void ConnectionHandler(object sender, MqttConnectionArgs args);

    public delegate void EventHandler<MqttMessageEventArgs>(object sender, MqttMessageEventArgs args);

    public delegate List<string> SubscriptionHandler(object sender, MqttMessageEventArgs args);

    public class MqttSession : IDisposable
    {
        private ConnectAckCode _code;

        private double _keepaliveSeconds;

        private string bootstrapToken;

        private SkunkLab.Security.Tokens.SecurityTokenType bootstrapTokenType;

        private bool disposed;

        //keepalive time increment
        private DateTime keepaliveExpiry;

        private Timer keepaliveTimer;

        private readonly PublishContainer pubContainer;

        //manages QoS 2 message features
        //timer for tracking keepalives
        //expiry of the keepalive
        private Dictionary<string, QualityOfServiceLevelType> qosLevels;

        private readonly MqttQuarantineTimer quarantine;

        public MqttSession(MqttConfig config)
        {
            Config = config;
            KeepAliveSeconds = config.KeepAliveSeconds;
            pubContainer = new PublishContainer(config);

            qosLevels = new Dictionary<string, QualityOfServiceLevelType>();
            quarantine = new MqttQuarantineTimer(config);
            quarantine.OnRetry += Quarantine_OnRetry;
        }

        public event ConnectionHandler OnConnect;                       //client function

        public event EventHandler<MqttMessageEventArgs> OnDisconnect;

        //client & server function
        public event EventHandler<MqttMessageEventArgs> OnKeepAlive;

        public event EventHandler<MqttMessageEventArgs> OnPublish;

        public event EventHandler<MqttMessageEventArgs> OnRetry;        //client function

        public event SubscriptionHandler OnSubscribe;                   //server function

        public event EventHandler<MqttMessageEventArgs> OnUnsubscribe;  //server function

        //server function
        //client function
        //public event EventHandler<MqttMessageEventArgs> OnKeepAliveExpiry; //server function

        public MqttConfig Config { get; set; }

        public ConnectAckCode ConnectResult
        {
            get => _code;
            internal set => _code = value;
        }

        //quarantines ids for reuse and supplies valid ids
        //qos levels return from subscriptions
        public bool HasBootstrapToken { get; internal set; }

        public string Identity { get; set; }

        public List<KeyValuePair<string, string>> Indexes { get; set; }

        public bool IsAuthenticated { get; set; }
        public bool IsConnected { get; internal set; }

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
            Disposing(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns a fresh usable message id.
        /// </summary>
        /// <returns></returns>
        public ushort NewId()
        {
            return quarantine.NewId();
        }

        /// <summary>
        /// Processes a receive MQTT message and a response or null (no response).
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<MqttMessage> ReceiveAsync(MqttMessage message)
        {
            MqttMessageHandler handler = MqttMessageHandler.Create(this, message);
            return await handler.ProcessAsync();
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
            else
            {
                return null;
            }
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
            //if the message is QoS = 2, the message is held waiting for release.
            if (message.QualityOfService != QualityOfServiceLevelType.ExactlyOnce
                || (message.QualityOfService == QualityOfServiceLevelType.ExactlyOnce && force))
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
            else
            {
                return null;
            }
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
            get => _keepaliveSeconds;
            set
            {
                _keepaliveSeconds = value;

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
            keepaliveExpiry = DateTime.UtcNow.AddSeconds(Convert.ToDouble(_keepaliveSeconds));
        }

        internal void StopKeepAlive()
        {
            keepaliveTimer.Stop();
            keepaliveTimer = null;
        }

        private void KeepaliveTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //DateTime expiry = keepaliveExpiry.AddSeconds(Convert.ToDouble(_keepaliveSeconds * 1.5));

            //communicates to server keep alive expired
            //if (expiry < DateTime.Now)
            //{
            //    OnKeepAliveExpiry?.Invoke(this, null);
            //return;
            //}

            //signals client to send a ping to keep alive
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

        #region Retry Signal

        /// <summary>
        /// Signals a retry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Quarantine_OnRetry(object sender, MqttMessageEventArgs args)
        {
            MqttMessage msg = args.Message;
            msg.Dup = true;
            OnRetry?.Invoke(this, new MqttMessageEventArgs(msg));
        }

        #endregion Retry Signal

        protected void Disposing(bool dispose)
        {
            if (dispose & !disposed)
            {
                quarantine.Dispose();
                pubContainer.Dispose();
                qosLevels.Clear();
                qosLevels = null;

                if (keepaliveTimer != null)
                {
                    keepaliveTimer.Dispose();
                }
            }

            disposed = true;
        }
    }
}