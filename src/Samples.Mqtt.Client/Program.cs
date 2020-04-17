using Newtonsoft.Json.Linq;
using Piraeus.Clients.Mqtt;
using SkunkLab.Channels;
using SkunkLab.Channels.WebSocket;
using SkunkLab.Protocols.Mqtt;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Samples.Mqtt.Client
{
    internal class Program
    {
        private static readonly string pubResource = null;

        private static readonly string subResource = null;

        private static string audience = issuer;

        private static IChannel channel;

        private static int channelNum;

        private static CancellationTokenSource cts;

        private static string hostname;

        private static int index;

        private static string issuer = "http://localhost/";

        private static PiraeusMqttClient mqttClient;

        private static string name;

        private static string nameClaimType = "http://localhost/name";

        private static string resourceA = "http://localhost/resource-a";

        private static string resourceB = "http://localhost/resource-b";

        private static string role;

        private static string roleClaimType = "http://localhost/role";

        private static bool send;

        private static string symmetricKey = "//////////////////////////////////////////8=";

        private static void Main(string[] args)
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            cts = new CancellationTokenSource();

            if (args == null || args.Length == 0)
            {
                UseUserInput();
            }
            else
            {
                Console.WriteLine("Invalid user input");
                Console.ReadKey();
                return;
            }

            string token = GetSecurityToken(name, role);

            channel = CreateChannel(token, cts);

            channel.OnClose += Channel_OnClose;
            channel.OnError += Channel_OnError;
            channel.OnOpen += Channel_OnOpen;

            mqttClient = new PiraeusMqttClient(new SkunkLab.Protocols.Mqtt.MqttConfig(180.0), channel);

            Task task = StartMqttClientAsync(token);
            Task.WaitAll(task);

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            cts.Cancel();
        }

        #region User Input

        private static void UseUserInput()
        {
            WriteHeader();
            if (File.Exists("config.json"))
            {
                Console.Write("Use config.json file [y/n] ? ");
                if (Console.ReadLine().ToLowerInvariant() == "y")
                {
                    JObject jobj = JObject.Parse(Encoding.UTF8.GetString(File.ReadAllBytes("config.json")));
                    string dnsName = jobj.Value<string>("dnsName");
                    string loc = jobj.Value<string>("location");
                    hostname = string.Format($"{dnsName}.{loc}.cloudapp.azure.com");
                    issuer = string.Format($"http://{hostname}/");
                    audience = issuer;
                    nameClaimType = jobj.Value<string>("identityClaimType");
                    roleClaimType = string.Format($"http://{hostname}/role");
                    symmetricKey = jobj.Value<string>("symmetricKey");
                    resourceA = $"http://{hostname}/resource-a";
                    resourceB = $"http://{hostname}/resource-b";
                }
                else
                {
                    hostname = SelectHostname();
                }
            }
            else
            {
                hostname = SelectHostname();
            }

            name = SelectName();
            role = SelectRole();
            channelNum = SelectChannel();
        }

        #endregion User Input

        #region Utilities

        private static void PrintMessage(string message, ConsoleColor color, bool section = false, bool input = false)
        {
            Console.ForegroundColor = color;
            if (section)
            {
                Console.WriteLine($"---   {message} ---");
            }
            else
            {
                if (!input)
                {
                    Console.WriteLine(message);
                }
                else
                {
                    Console.Write(message);
                }
            }

            Console.ResetColor();
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception.Flatten().InnerException.Message);
        }

        private static void WriteHeader()
        {
            PrintMessage("-------------------------------------------------------------------", ConsoleColor.White);
            PrintMessage("                       MQTT Sample Client", ConsoleColor.Cyan);
            PrintMessage("-------------------------------------------------------------------", ConsoleColor.White);
            PrintMessage("press any key to continue...", ConsoleColor.White);
            Console.ReadKey();
        }

        #endregion Utilities

        #region MQTT Client

        private static async Task<ConnectAckCode> MqttConnectAsync(string token)
        {
            PrintMessage("Trying to connect", ConsoleColor.Cyan, true);
            string sessionId = Guid.NewGuid().ToString();
            ConnectAckCode code = await mqttClient.ConnectAsync(sessionId, "JWT", token, 90);
            PrintMessage($"MQTT connection code {code}", code == ConnectAckCode.ConnectionAccepted ? ConsoleColor.Green : ConsoleColor.Red, false);

            return code;
        }

        private static void ObserveEvent(string topic, string contentType, byte[] message)
        {
            long nowTicks = DateTime.Now.Ticks;
            Console.ForegroundColor = ConsoleColor.Green;
            string msg = Encoding.UTF8.GetString(message);
            string[] split = msg.Split(":", StringSplitOptions.RemoveEmptyEntries);
            string ticksString = split[0];
            long sendTicks = Convert.ToInt64(ticksString);
            long ticks = nowTicks - sendTicks;
            TimeSpan latency = TimeSpan.FromTicks(ticks);
            string messageText = msg.Replace(split[0], "").Trim(new char[] { ':', ' ' });

            Console.WriteLine($"Latency {latency.TotalMilliseconds} ms - Received message '{messageText}'");
        }

        private static void SendMessages(Task task)
        {
            try
            {
                if (!send)
                {
                    PrintMessage("Do you want to send messages (Y/N) ? ", ConsoleColor.Cyan, false, true);
                    string sendVal = Console.ReadLine();
                    if (sendVal.ToUpperInvariant() != "Y")
                    {
                        return;
                    }
                }
                send = true;

                PrintMessage("Enter # of messages to send ? ", ConsoleColor.Cyan, false, true);
                string nstring = Console.ReadLine();

                int numMessages = int.Parse(nstring);
                PrintMessage("Enter delay between messages in milliseconds ? ", ConsoleColor.Cyan, false, true);
                string dstring = Console.ReadLine().Trim();

                int delayms = int.Parse(dstring);

                DateTime startTime = DateTime.Now;
                for (int i = 0; i < numMessages; i++)
                {
                    index++;
                    string payloadString = string.Format($"{DateTime.Now.Ticks}:{name}-message {index}");
                    byte[] payload = Encoding.UTF8.GetBytes(payloadString);
                    string publishEvent = !string.IsNullOrEmpty(pubResource) ? pubResource : role == "A" ? resourceA : resourceB;
                    Task pubTask = mqttClient.PublishAsync(QualityOfServiceLevelType.AtMostOnce, publishEvent, "text/plain", payload);
                    Task.WhenAll(pubTask);

                    if (delayms > 0)
                    {
                        Task t = Task.Delay(delayms);
                        Task.WaitAll(t);
                    }
                }

                DateTime endTime = DateTime.Now;
                PrintMessage($"Total send time {endTime.Subtract(startTime).TotalMilliseconds} ms", ConsoleColor.White);

                PrintMessage("Send more messages (Y/N) ? ", ConsoleColor.Cyan, false, true);
                string val = Console.ReadLine();
                if (val.ToUpperInvariant() == "Y")
                {
                    SendMessages(task);
                }
            }
            catch (Exception ex)
            {
                PrintMessage("Error", ConsoleColor.Red, true);
                PrintMessage(ex.Message, ConsoleColor.Red);
                Console.ReadKey();
            }
        }

        private static async Task StartMqttClientAsync(string token)
        {
            ConnectAckCode code = await MqttConnectAsync(token);
            if (code != ConnectAckCode.ConnectionAccepted)
            {
                return;
            }

            string observableEvent = !string.IsNullOrEmpty(pubResource) ? subResource : role == "A" ? resourceB : resourceA;
            try
            {
                await mqttClient.SubscribeAsync(observableEvent, QualityOfServiceLevelType.AtLeastOnce, ObserveEvent).ContinueWith(SendMessages);
            }
            catch (Exception ex)
            {
                PrintMessage("Error", ConsoleColor.Red, true);
                PrintMessage(ex.Message, ConsoleColor.Red);
                Console.ReadKey();
            }
        }

        #endregion MQTT Client

        #region Channel Events

        private static void Channel_OnClose(object sender, ChannelCloseEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Channel '{e.ChannelId}' is closed");
        }

        private static void Channel_OnError(object sender, ChannelErrorEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Channel '{e.ChannelId}' error '{e.Error.Message}'");
            Console.ResetColor();
        }

        private static void Channel_OnOpen(object sender, ChannelOpenEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Channel '{e.ChannelId}' is open");
            Console.ResetColor();
        }

        private static void Channel_OnStateChange(object sender, ChannelStateEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Channel '{e.ChannelId}' state changed to '{e.State}'");
            Console.ResetColor();
        }

        #endregion Channel Events

        #region Inputs

        private static int SelectChannel()
        {
            string chn = "1";
            if (chn == "1")
            {
                return Convert.ToInt32(chn);
            }
            else
            {
                return SelectChannel();
            }
        }

        private static string SelectHostname()
        {
            Console.Write("Enter hostname, IP, or Enter for localhost ? ");
            string hostname = Console.ReadLine();
            if (string.IsNullOrEmpty(hostname))
            {
                return "localhost";
            }
            else
            {
                return hostname;
            }
        }

        private static string SelectName()
        {
            Console.Write("Enter name for this client ? ");
            return Console.ReadLine();
        }

        private static string SelectRole()
        {
            Console.Write("Enter role for the client (A/B) ? ");
            string role = Console.ReadLine().ToUpperInvariant();
            if (role == "A" || role == "B")
            {
                return role;
            }
            else
            {
                return SelectRole();
            }
        }

        #endregion Inputs

        #region Security Token

        private static string CreateJwt(string audience, string issuer, List<Claim> claims, string symmetricKey, double lifetimeMinutes)
        {
            SkunkLab.Security.Tokens.JsonWebToken jwt = new SkunkLab.Security.Tokens.JsonWebToken(new Uri(audience), symmetricKey, issuer, claims, lifetimeMinutes);
            return jwt.ToString();
        }

        private static string GetSecurityToken(string name, string role)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim(nameClaimType, name),
                new Claim(roleClaimType, role)
            };

            return CreateJwt(audience, issuer, claims, symmetricKey, 60.0);
        }

        #endregion Security Token

        #region Channels

        public static IChannel CreateChannel(string token, CancellationTokenSource src)
        {
            if (channelNum == 1)
            {
                string uriString = hostname == "localhost" ? "ws://localhost:8081/api/connect" : string.Format("wss://{0}/ws/api/connect", hostname);

                Uri uri = new Uri(uriString);
                return ChannelFactory.Create(uri, token, "mqtt", new WebSocketConfig(), src.Token);
            }
            else
            {
                if (hostname != "localhost")
                {
                    hostname = string.Format("{0}/tcp", hostname);
                }

                return ChannelFactory.Create(false, hostname, 8883, 2048, 2048 * 10, src.Token);
            }
        }

        #endregion Channels
    }
}