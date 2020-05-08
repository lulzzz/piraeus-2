using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using SkunkLab.Channels;
using SkunkLab.Protocols;
using SkunkLab.Protocols.Mqtt;
using SkunkLab.Protocols.Mqtt.Handlers;
using SkunkLab.Protocols.Utilities;

namespace Piraeus.Clients.Mqtt
{
    public delegate void MqttClientChannelErrorHandler(object sender, ChannelErrorEventArgs args);

    public delegate void MqttClientChannelStateHandler(object sender, ChannelStateEventArgs args);

    public class PiraeusMqttClient
    {
        private readonly IMqttDispatch dispatcher;

        private readonly Queue<byte[]> queue;

        private readonly double timeoutMilliseconds;

        private ConnectAckCode? code;

        private MqttSession session;

        public PiraeusMqttClient(MqttConfig config, IChannel channel, IMqttDispatch dispatcher = null)
        {
            this.dispatcher = dispatcher ?? new GenericMqttDispatcher();
            timeoutMilliseconds = config.MaxTransmitSpan.TotalMilliseconds;
            session = new MqttSession(config);
            session.OnKeepAlive += Session_OnKeepAlive;
            session.OnConnect += Session_OnConnect;
            session.OnDisconnect += Session_OnDisconnect;
            session.OnRetry += Session_OnRetry;

            Channel = channel;
            Channel.OnReceive += Channel_OnReceive;
            Channel.OnClose += Channel_OnClose;
            Channel.OnError += Channel_OnError;
            Channel.OnStateChange += Channel_OnStateChange;

            queue = new Queue<byte[]>();
        }

        public event MqttClientChannelErrorHandler OnChannelError;

        public event MqttClientChannelStateHandler OnChannelStateChange;

        public IChannel Channel
        {
            get;
        }

        public bool IsConnected => Channel.IsConnected;

        public async Task CloseAsync()
        {
            if (session != null)
            {
                session.Dispose();
                session = null;
            }

            try
            {
                Channel.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Channel close exception {0}", ex.Message);
                Console.WriteLine("Channel close exception stack trace {0}", ex.StackTrace);
            }

            await Task.CompletedTask;
        }

        public async Task<ConnectAckCode> ConnectAsync(string clientId, string username, string password,
            int keepaliveSeconds)
        {
            code = null;

            ConnectMessage msg = new ConnectMessage(clientId, username, password, keepaliveSeconds, true);

            if (!Channel.IsConnected)
            {
                try
                {
                    await Channel.OpenAsync();
                }
                catch (Exception ex)
                {
                    OnChannelError?.Invoke(this, new ChannelErrorEventArgs(Channel.Id, ex));
                    return ConnectAckCode.ServerUnavailable;
                }

                try
                {
                    Receive(Channel);
                }
                catch (Exception ex)
                {
                    OnChannelError?.Invoke(this, new ChannelErrorEventArgs(Channel.Id, ex));
                    return ConnectAckCode.ServerUnavailable;
                }
            }

            try
            {
                await Channel.SendAsync(msg.Encode());

                DateTime expiry = DateTime.UtcNow.AddMilliseconds(timeoutMilliseconds);
                while (!code.HasValue)
                {
                    await Task.Delay(10);
                    if (DateTime.UtcNow > expiry)
                    {
                        throw new TimeoutException("MQTT connection timed out.");
                    }
                }

                return code.Value;
            }
            catch (Exception ex)
            {
                OnChannelError?.Invoke(this, new ChannelErrorEventArgs(Channel.Id, ex));
                return ConnectAckCode.ServerUnavailable;
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                string id = Channel.Id;
                DisconnectMessage msg = new DisconnectMessage();

                if (Channel.IsConnected)
                {
                    await Channel.SendAsync(msg.Encode());
                }
            }
            catch (Exception ex)
            {
                string disconnectMsgError = string.Format("ERROR: Sending MQTT Disconnect message '{0}'", ex.Message);
                Console.WriteLine(disconnectMsgError);
                Trace.TraceError(disconnectMsgError);
            }

            try
            {
                Channel.Dispose();
            }
            catch (Exception ex)
            {
                string channelDisposeMsgError =
                    string.Format("ERROR: MQTT channel dispose after disconnect '{0}'", ex.Message);
                Console.WriteLine(channelDisposeMsgError);
                Trace.TraceError(channelDisposeMsgError);
            }

            try
            {
                if (session != null)
                {
                    session.Dispose();
                    session = null;
                }
            }
            catch (Exception ex)
            {
                string sessionDisposeMsgError =
                    string.Format("ERROR: MQTT session dispose after disconnect '{0}'", ex.Message);
                Console.WriteLine(sessionDisposeMsgError);
                Trace.TraceError(sessionDisposeMsgError);
            }
        }

        public async Task PublishAsync(QualityOfServiceLevelType qos, string topicUriString, string contentType,
            byte[] data, string cacheKey = null, IEnumerable<KeyValuePair<string, string>> indexes = null,
            string messageId = null)
        {
            try
            {
                string indexString = GetIndexString(indexes);

                UriBuilder builder = new UriBuilder(topicUriString);
                string queryString = messageId == null
                    ? string.Format("{0}={1}", QueryStringConstants.CONTENT_TYPE, contentType)
                    : string.Format("{0}={1}&{2}={3}", QueryStringConstants.CONTENT_TYPE, contentType,
                        QueryStringConstants.MESSAGE_ID, messageId);

                if (!string.IsNullOrEmpty(cacheKey))
                {
                    queryString += string.Format("&{0}={1}", QueryStringConstants.CACHE_KEY, cacheKey);
                }

                if (!string.IsNullOrEmpty(indexString))
                {
                    queryString = queryString + "&" + indexString;
                }

                builder.Query = queryString;

                PublishMessage msg =
                    new PublishMessage(false, qos, false, 0, builder.ToString().ToLowerInvariant(), data);
                if (qos != QualityOfServiceLevelType.AtMostOnce)
                {
                    msg.MessageId = session.NewId();
                    session.Quarantine(msg, DirectionType.Out);
                }

                queue.Enqueue(msg.Encode());

                while (queue.Count > 0)
                {
                    byte[] message = queue.Dequeue();

                    await Channel.SendAsync(message);
                }
            }
            catch (Exception ex)
            {
                OnChannelError?.Invoke(this, new ChannelErrorEventArgs(Channel.Id, ex));
            }
        }

        public void RegisterTopic(string topic, Action<string, string, byte[]> action)
        {
            Uri uri = new Uri(topic.ToLowerInvariant());
            dispatcher.Register(uri.ToString(), action);
        }

        public async Task SubscribeAsync(string topicUriString, QualityOfServiceLevelType qos,
            Action<string, string, byte[]> action)
        {
            try
            {
                Dictionary<string, QualityOfServiceLevelType> dict = new Dictionary<string, QualityOfServiceLevelType> {
                    {topicUriString.ToLowerInvariant(), qos}
                };
                dispatcher.Register(topicUriString.ToLowerInvariant(), action);
                SubscribeMessage msg = new SubscribeMessage(session.NewId(), dict);
                await Channel.SendAsync(msg.Encode());
            }
            catch (Exception ex)
            {
                OnChannelError?.Invoke(this, new ChannelErrorEventArgs(Channel.Id, ex));
            }
        }

        public async Task SubscribeAsync(
            Tuple<string, QualityOfServiceLevelType, Action<string, string, byte[]>>[] subscriptions)
        {
            try
            {
                Dictionary<string, QualityOfServiceLevelType>
                    dict = new Dictionary<string, QualityOfServiceLevelType>();

                foreach (var tuple in subscriptions)
                {
                    dict.Add(tuple.Item1, tuple.Item2);
                    dispatcher.Register(tuple.Item1, tuple.Item3);
                }

                SubscribeMessage msg = new SubscribeMessage(session.NewId(), dict);

                await Channel.SendAsync(msg.Encode());
            }
            catch (Exception ex)
            {
                OnChannelError?.Invoke(this, new ChannelErrorEventArgs(Channel.Id, ex));
            }
        }

        public void UnregisterTopic(string topic)
        {
            Uri uri = new Uri(topic.ToLowerInvariant());
            dispatcher.Unregister(uri.ToString());
        }

        public async Task UnsubscribeAsync(string topic)
        {
            try
            {
                UnsubscribeMessage msg = new UnsubscribeMessage(session.NewId(), new[] { topic });

                await Channel.SendAsync(msg.Encode());
                dispatcher.Unregister(topic);
            }
            catch (Exception ex)
            {
                OnChannelError?.Invoke(this, new ChannelErrorEventArgs(Channel.Id, ex));
            }
        }

        public async Task UnsubscribeAsync(IEnumerable<string> topics)
        {
            try
            {
                UnsubscribeMessage msg = new UnsubscribeMessage(session.NewId(), topics);

                await Channel.SendAsync(msg.Encode());
                foreach (string topic in topics)
                    dispatcher.Unregister(topic);
            }
            catch (Exception ex)
            {
                OnChannelError?.Invoke(this, new ChannelErrorEventArgs(Channel.Id, ex));
            }
        }

        private void Receive(IChannel channel)
        {
            try
            {
                Task task = channel.ReceiveAsync();
                Task.WhenAll(task);
            }
            catch (AggregateException ae)
            {
                Console.WriteLine("Receive AggregateException '{0}'", ae.Flatten().InnerException.Message);
            }
        }

        #region Channel Events

        private void Channel_OnClose(object sender, ChannelCloseEventArgs args)
        {
            try
            {
                code = null;
                Channel.Dispose();
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Piraeus MQTT client fault disposing channel.");
                Trace.TraceError(ex.Message);
            }
        }

        private void Channel_OnError(object sender, ChannelErrorEventArgs args)
        {
            OnChannelError?.Invoke(this, args);
        }

        private void Channel_OnReceive(object sender, ChannelReceivedEventArgs args)
        {
            MqttMessage msg = MqttMessage.DecodeMessage(args.Message);
            MqttMessageHandler handler = MqttMessageHandler.Create(session, msg, dispatcher);

            try
            {
                MqttMessage message = handler.ProcessAsync().GetAwaiter().GetResult();
                if (message != null)
                {
                    Channel.SendAsync(message.Encode()).GetAwaiter();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Trace.TraceError(ex.Message);
                OnChannelError?.Invoke(this, new ChannelErrorEventArgs(Channel.Id, ex));
            }
        }

        private void Channel_OnStateChange(object sender, ChannelStateEventArgs args)
        {
            OnChannelStateChange?.Invoke(this, args);
        }

        private string GetIndexString(IEnumerable<KeyValuePair<string, string>> indexes = null)
        {
            if (indexes == null)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            foreach (KeyValuePair<string, string> kvp in indexes)
            {
                if (builder.ToString().Length == 0)
                {
                    builder.Append(string.Format("i={0};{1}", kvp.Key, kvp.Value));
                }
                else
                {
                    builder.Append(string.Format("&i={0};{1}", kvp.Key, kvp.Value));
                }
            }

            return builder.ToString();
        }

        #endregion Channel Events

        #region Session Events

        private void Session_OnConnect(object sender, MqttConnectionArgs args)
        {
            code = args.Code;
        }

        private void Session_OnDisconnect(object sender, MqttMessageEventArgs args)
        {
            Channel.CloseAsync().GetAwaiter();
            Channel.Dispose();
        }

        private void Session_OnKeepAlive(object sender, MqttMessageEventArgs args)
        {
            try
            {
                Task task = Channel.SendAsync(args.Message.Encode());
                if (Channel.RequireBlocking)
                {
                    Task.WaitAll(task);
                }
                else
                {
                    Task.WhenAll(task);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.InnerException.Message);
                Console.ResetColor();
            }
        }

        private void Session_OnRetry(object sender, MqttMessageEventArgs args)
        {
            MqttMessage msg = args.Message;
            msg.Dup = true;
            Channel.SendAsync(msg.Encode()).GetAwaiter();
        }

        #endregion Session Events
    }
}