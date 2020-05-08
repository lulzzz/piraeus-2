using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Piraeus.Clients.Rest;
using SkunkLab.Channels;
using SkunkLab.Security.Tokens;

namespace Samples.Http.Client
{
    internal class Program
    {
        private static string audience = issuer;

        private static CancellationTokenSource cts;

        private static string hostname;

        private static string issuer = "http://localhost/";

        private static string name;

        private static string nameClaimType = "http://localhost/name";

        private static string resourceA = "http://localhost/resource-a";

        private static string resourceB = "http://localhost/resource-b";

        private static RestClient restClient;

        private static string role;

        private static string roleClaimType = "http://localhost/role";

        private static DateTime startTime;

        private static string symmetricKey = "//////////////////////////////////////////8=";

        private static string CreateJwt(string audience, string issuer, List<Claim> claims, string symmetricKey,
            double lifetimeMinutes)
        {
            JsonWebToken jwt = new JsonWebToken(new Uri(audience), symmetricKey, issuer, claims, lifetimeMinutes);
            return jwt.ToString();
        }

        private static string GetSecurityToken(string name, string role)
        {
            //Normally a security token would be obtained externally
            //For the sample we are going to build a token that can
            //be authn'd and authz'd for this sample

            //string issuer = "http://skunklab.io/";
            //string audience = issuer;
            //string nameClaimType = "http://skunklab.io/name";
            //string roleClaimType = "http://skunklab.io/role";
            //string symmetricKey = "//////////////////////////////////////////8=";

            List<Claim> claims = new List<Claim> {
                new Claim(nameClaimType, name),
                new Claim(roleClaimType, role)
            };

            return CreateJwt(audience, issuer, claims, symmetricKey, 60.0);
        }

        private static async Task LongPollAsync(string requestUri)
        {
            while (true)
            {
                HttpClient client = new HttpClient();

                HttpResponseMessage message = await client.GetAsync(requestUri);
                if (message.StatusCode == HttpStatusCode.OK ||
                    message.StatusCode == HttpStatusCode.Accepted)
                {
                    try
                    {
                        long nowTicks = DateTime.Now.Ticks;
                        Console.ForegroundColor = ConsoleColor.Green;
                        string msg = await message.Content.ReadAsStringAsync();
                        string[] split = msg.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        string ticksString = split[0];
                        long sendTicks = Convert.ToInt64(ticksString);
                        long ticks = nowTicks - sendTicks;
                        TimeSpan latency = TimeSpan.FromTicks(ticks);
                        string messageText = msg.Replace(split[0], "").Trim(':', ' ');

                        Console.WriteLine($"Latency {latency.TotalMilliseconds} ms - Received message '{messageText}'");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Cannot read message");
                    }
                }
                else
                {
                    Console.WriteLine(message.StatusCode);
                }
            }
        }

        private static void Main(string[] args)
        {
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

            string endpoint = hostname == "localhost"
                ? $"http://{hostname}:8088/api/connect"
                : $"https://{hostname}/api/connect";
            string qs = role.ToUpperInvariant() == "A" ? resourceA : resourceB;
            string requestUriString = $"{endpoint}?r={qs}";
            string sub = role.ToUpperInvariant() == "A" ? resourceB : resourceA;
            string pollUriString = $"{endpoint}?sub={sub}";
            string token = GetSecurityToken(name, role);

            Uri observableResource = role.ToUpperInvariant() == "A" ? new Uri(resourceB) : new Uri(resourceA);
            Observer observer = new HttpObserver(observableResource);
            observer.OnNotify += Observer_OnNotify;
            restClient = new RestClient(endpoint, token, new[] { observer }, cts.Token);

            RunAsync().Wait();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            cts.Cancel();
        }

        private static void Observer_OnNotify(object sender, ObserverEventArgs args)
        {
            long nowTicks = DateTime.Now.Ticks;
            Console.ForegroundColor = ConsoleColor.Green;
            string msg = Encoding.UTF8.GetString(args.Message);
            string[] split = msg.Split(":", StringSplitOptions.RemoveEmptyEntries);
            string ticksString = split[0];
            long sendTicks = Convert.ToInt64(ticksString);
            long ticks = nowTicks - sendTicks;
            TimeSpan latency = TimeSpan.FromTicks(ticks);
            string messageText = msg.Replace(split[0], "").Trim(':', ' ');

            Console.WriteLine($"Latency {latency.TotalMilliseconds} ms - Received message '{messageText}'");
        }

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

        private static async Task RunAsync()
        {
            int index = 0;
            bool running = true;
            while (running)
            {
                PrintMessage("Do you want to send messages (Y/N) ? ", ConsoleColor.Cyan, false, true);
                string sendVal = Console.ReadLine().Trim();
                if (sendVal.ToUpperInvariant() != "Y")
                {
                    break;
                }

                PrintMessage("Enter # of messages to send ? ", ConsoleColor.Cyan, false, true);
                string nstring = Console.ReadLine();
                if (int.TryParse(nstring, out int num))
                {
                    PrintMessage("Enter delay between messages in milliseconds ? ", ConsoleColor.Cyan, false, true);
                    string dstring = Console.ReadLine().Trim();
                    if (int.TryParse(dstring, out int delay))
                    {
                        startTime = DateTime.Now;

                        for (int i = 0; i < num; i++)
                        {
                            string payloadString = string.Format($"{DateTime.Now.Ticks}:{name}-message {index++}");
                            byte[] payload = Encoding.UTF8.GetBytes(payloadString);
                            string publishEvent = role == "A" ? resourceA : resourceB;
                            restClient.SendAsync(publishEvent, "text/plain", payload).GetAwaiter();

                            await Task.Delay(delay);
                        }

                        DateTime endTime = DateTime.Now;
                        PrintMessage($"Total send time {endTime.Subtract(startTime).TotalMilliseconds} ms",
                            ConsoleColor.White);
                    }
                }
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

            return hostname;
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

            return SelectRole();
        }

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
        }

        private static void WriteHeader()
        {
            PrintMessage("-------------------------------------------------------------------", ConsoleColor.White);
            PrintMessage("                       HTTP Sample Client", ConsoleColor.Cyan);
            PrintMessage("-------------------------------------------------------------------", ConsoleColor.White);
            PrintMessage("press any key to continue...", ConsoleColor.White);
            Console.ReadKey();
        }
    }
}