using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SkunkLab.Channels;
using SkunkLab.Protocols;
using SkunkLab.Protocols.Mqtt;
using SkunkLab.Protocols.Mqtt.Handlers;

namespace Piraeus.Clients.Mqtt
{
    public class GenericMqttClient
    {
        private readonly IChannel channel;

        private readonly IMqttDispatch dispatcher;

        private readonly MqttSession session;

        public GenericMqttClient(MqttConfig config, IChannel channel, IMqttDispatch dispatcher = null)
        {
            this.dispatcher = dispatcher ?? new GenericMqttDispatcher();
            session = new MqttSession(config);
            session.OnConnect += Session_OnConnect;
            session.OnDisconnect += Session_OnDisconnect;
            session.OnRetry += Session_OnRetry;

            this.channel = channel;
            this.channel.OnReceive += Channel_OnReceive;
            this.channel.OnClose += Channel_OnClose;
            this.channel.OnError += Channel_OnError;
            this.channel.OnStateChange += Channel_OnStateChange;
        }

        public event MqttClientChannelErrorHandler OnChannelError;

        public event MqttClientChannelStateHandler OnChannelStateChange;

        public bool ChannelConnected => channel.IsConnected;

        public ConnectAckCode? MqttConnectCode
        {
            get; private set;
        }

        #region MQTT Functions

        public async Task ConnectAsync(string clientId, string username, string password, int keepaliveSeconds,
            bool cleanSession)
        {
            MqttConnectCode = null;

            ConnectMessage msg = new ConnectMessage(clientId, username, password, keepaliveSeconds, cleanSession);

            if (!channel.IsConnected)
            {
                await channel.OpenAsync();
                Task task = channel.ReceiveAsync();
                await Task.WhenAll(task);
            }

            await channel.SendAsync(msg.Encode());
        }

        public async Task DisconnectAsync()
        {
            DisconnectMessage msg = new DisconnectMessage();
            await channel.SendAsync(msg.Encode());
        }

        public async Task PublishAsync(string topic, QualityOfServiceLevelType qos, bool retain, bool dup, byte[] data)
        {
            ushort id = session.NewId();
            PublishMessage msg = new PublishMessage(dup, qos, retain, id, topic, data);

            if (qos != QualityOfServiceLevelType.AtMostOnce)
            {
                session.Quarantine(msg, DirectionType.In);
            }

            await channel.SendAsync(msg.Encode());
        }

        public async Task SubscribeAsync(string topic, QualityOfServiceLevelType qos,
            Action<string, string, byte[]> action)
        {
            Dictionary<string, QualityOfServiceLevelType> dict = new Dictionary<string, QualityOfServiceLevelType> {
                {topic, qos}
            };
            dispatcher.Register(topic, action);
            SubscribeMessage msg = new SubscribeMessage(session.NewId(), dict);
            await channel.SendAsync(msg.Encode());
        }

        public async Task SubscribeAsync(
            Tuple<string, QualityOfServiceLevelType, Action<string, string, byte[]>>[] subscriptions)
        {
            Dictionary<string, QualityOfServiceLevelType> dict = new Dictionary<string, QualityOfServiceLevelType>();

            foreach (var tuple in subscriptions)
            {
                dict.Add(tuple.Item1, tuple.Item2);
                dispatcher.Register(tuple.Item1, tuple.Item3);
            }

            SubscribeMessage msg = new SubscribeMessage(session.NewId(), dict);
            await channel.SendAsync(msg.Encode());
        }

        public async Task UnsubscribeAsync(string topic)
        {
            UnsubscribeMessage msg = new UnsubscribeMessage(session.NewId(), new[] { topic });
            await channel.SendAsync(msg.Encode());
            dispatcher.Unregister(topic);
        }

        public async Task UnsubscribeAsync(IEnumerable<string> topics)
        {
            UnsubscribeMessage msg = new UnsubscribeMessage(session.NewId(), topics);
            await channel.SendAsync(msg.Encode());

            foreach (string topic in topics)
                dispatcher.Unregister(topic);
        }

        #endregion MQTT Functions

        #region Channel Events

        private void Channel_OnClose(object sender, ChannelCloseEventArgs args)
        {
            MqttConnectCode = null;
        }

        private void Channel_OnError(object sender, ChannelErrorEventArgs args)
        {
            OnChannelError?.Invoke(this, args);
        }

        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs args)
        {
            MqttMessage msg = MqttMessage.DecodeMessage(args.Message);
            MqttMessageHandler handler = MqttMessageHandler.Create(session, msg);

            MqttMessage response = handler.ProcessAsync().GetAwaiter().GetResult();

            if (response != null)
            {
                channel.SendAsync(response.Encode()).GetAwaiter();
            }
        }

        private void Channel_OnStateChange(object sender, ChannelStateEventArgs args)
        {
            OnChannelStateChange?.Invoke(this, args);
        }

        #endregion Channel Events

        #region Session Events

        private void Session_OnConnect(object sender, MqttConnectionArgs args)
        {
            MqttConnectCode = args.Code;
        }

        private void Session_OnDisconnect(object sender, MqttMessageEventArgs args)
        {
            channel.CloseAsync().GetAwaiter();
            channel.Dispose();
        }

        private void Session_OnRetry(object sender, MqttMessageEventArgs args)
        {
            MqttMessage msg = args.Message;
            msg.Dup = true;
            channel.SendAsync(msg.Encode()).GetAwaiter();
        }

        #endregion Session Events
    }
}