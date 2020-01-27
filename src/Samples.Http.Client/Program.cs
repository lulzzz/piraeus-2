using Newtonsoft.Json.Linq;
using Piraeus.Clients.Rest;
using SkunkLab.Channels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Samples.Http.Client
{
    class Program
    {
        static CancellationTokenSource cts;
        static RestClient restClient;
        static int index;
        static IChannel channel;
        static string name;
        static string role;
        static bool send;
        static int channelNum;
        static string hostname;
        static string resourceA = "http://localhost/resource-a";
        static string resourceB = "http://localhost/resource-b";
        static readonly string pubResource = null;
        static readonly string subResource = null;
        static string issuer = "http://localhost/";
        static string audience = issuer;
        static string nameClaimType = "http://localhost/name";
        static string roleClaimType = "http://localhost/role";
        static string symmetricKey = "//////////////////////////////////////////8=";
        static DateTime startTime;

        static void Main(string[] args)
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

            string endpoint = hostname == "localhost" ? $"http://{hostname}:8088/api/connect" : $"https://{hostname}/api/connect";
            string qs = role.ToUpperInvariant() == "A" ? resourceA : resourceB;
            string requestUriString = $"{endpoint}?r={qs}";
            string sub = role.ToUpperInvariant() == "A" ? resourceB : resourceA;
            string pollUriString = $"{endpoint}?sub={sub}";
            string token = GetSecurityToken(name, role);
           

            Uri observableResource = role.ToUpperInvariant() == "A" ? new Uri(resourceB) : new Uri(resourceA);
            Observer observer = new HttpObserver(observableResource);
            observer.OnNotify += Observer_OnNotify;
            restClient = new RestClient(endpoint, token, new Observer[] { observer }, cts.Token);


            RunAsync().Wait();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            cts.Cancel();


        }

        private static async Task LongPollAsync(string requestUri)
        {
            while (true)
            {
                HttpClient client = new HttpClient();

                HttpResponseMessage message = await client.GetAsync(requestUri);
                if (message.StatusCode == System.Net.HttpStatusCode.OK ||
                    message.StatusCode == System.Net.HttpStatusCode.Accepted)
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
                        string messageText = msg.Replace(split[0], "").Trim(new char[] { ':', ' ' });

                        Console.WriteLine($"Latency {latency.TotalMilliseconds} ms - Received message '{messageText}'");
                    }
                    catch(Exception ex)
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
        

        private static async Task RunAsync()
        {
            int index = 0;
            bool running = true;
            while(running)
            {
                PrintMessage("Do you want to send messages (Y/N) ? ", ConsoleColor.Cyan, false, true);
                string sendVal = Console.ReadLine().Trim();
                if (sendVal.ToUpperInvariant() != "Y")
                    break;

                PrintMessage("Enter # of messages to send ? ", ConsoleColor.Cyan, false, true);
                string nstring = Console.ReadLine();
                int num = 0;
                if(Int32.TryParse(nstring, out num))
                {
                    PrintMessage("Enter delay between messages in milliseconds ? ", ConsoleColor.Cyan, false, true);
                    string dstring = Console.ReadLine().Trim();
                    int delay = 0;
                    if(Int32.TryParse(dstring, out delay))
                    {
                        startTime = DateTime.Now;
                        
                        for (int i = 0; i < num; i++)
                        {
                            string payloadString = String.Format($"{DateTime.Now.Ticks}:{name}-message {index++}");
                            byte[] payload = Encoding.UTF8.GetBytes(payloadString);
                            string publishEvent = role == "A" ? resourceA : resourceB;
                            restClient.SendAsync(publishEvent, "text/plain", payload).GetAwaiter();

                            await Task.Delay(delay);
                        }

                        DateTime endTime = DateTime.Now;
                        PrintMessage($"Total send time {endTime.Subtract(startTime).TotalMilliseconds} ms", ConsoleColor.White);
                    }
                }                
            }
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
            string messageText = msg.Replace(split[0], "").Trim(new char[] { ':', ' ' });

            Console.WriteLine($"Latency {latency.TotalMilliseconds} ms - Received message '{messageText}'");
        }

        
        static void UseUserInput()
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
                    hostname = String.Format($"{dnsName}.{loc}.cloudapp.azure.com");
                    issuer = String.Format($"http://{hostname}/");
                    audience = issuer;
                    nameClaimType = jobj.Value<string>("identityClaimType");
                    roleClaimType = String.Format($"http://{hostname}/role");
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

        static void WriteHeader()
        {
            PrintMessage("-------------------------------------------------------------------", ConsoleColor.White);
            PrintMessage("                       HTTP Sample Client", ConsoleColor.Cyan);
            PrintMessage("-------------------------------------------------------------------", ConsoleColor.White);
            PrintMessage("press any key to continue...", ConsoleColor.White);
            Console.ReadKey();
        }

        static void PrintMessage(string message, ConsoleColor color, bool section = false, bool input = false)
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

        static string SelectHostname()
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

        static string SelectName()
        {
            Console.Write("Enter name for this client ? ");
            return Console.ReadLine();
        }

        static string SelectRole()
        {
            Console.Write("Enter role for the client (A/B) ? ");
            string role = Console.ReadLine().ToUpperInvariant();
            if (role == "A" || role == "B")
                return role;
            else
                return SelectRole();
        }

        static string GetSecurityToken(string name, string role)
        {
            //Normally a security token would be obtained externally
            //For the sample we are going to build a token that can
            //be authn'd and authz'd for this sample

            //string issuer = "http://skunklab.io/";
            //string audience = issuer;
            //string nameClaimType = "http://skunklab.io/name";
            //string roleClaimType = "http://skunklab.io/role";
            //string symmetricKey = "//////////////////////////////////////////8=";


            List<Claim> claims = new List<Claim>()
            {
                new Claim(nameClaimType, name),
                new Claim(roleClaimType, role)
            };

            return CreateJwt(audience, issuer, claims, symmetricKey, 60.0);
        }

        static string CreateJwt(string audience, string issuer, List<Claim> claims, string symmetricKey, double lifetimeMinutes)
        {
            SkunkLab.Security.Tokens.JsonWebToken jwt = new SkunkLab.Security.Tokens.JsonWebToken(new Uri(audience), symmetricKey, issuer, claims, lifetimeMinutes);
            return jwt.ToString();
        }

    }
}
