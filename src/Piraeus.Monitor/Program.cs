using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Piraeus.Monitor
{
    public class Program
    {
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.ConfigureKestrel(options =>
                    {
                        string hostname = Dns.GetHostName();
                        Console.WriteLine($"Hostname = {hostname}");
                        IPAddress address = GetIPAddress(hostname);
                        if (hostname == "localhost")
                        {
                            options.Listen(address, 44376);
                        }
                        else
                        {
                            options.ListenAnyIP(8087);
                            Console.WriteLine("Listening on 8080");
                        }
                    });
                    webBuilder.UseStartup<Startup>();
                });
        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        private static IPAddress GetIPAddress(string hostname)
        {
            IPHostEntry hostInfo = Dns.GetHostEntry(hostname);
            for (int index = 0; index < hostInfo.AddressList.Length; index++)
            {
                if (hostInfo.AddressList[index].AddressFamily == AddressFamily.InterNetwork)
                {
                    return hostInfo.AddressList[index];
                }
            }

            return null;
        }

        //public static IHostBuilder CreateHostBuilder(string[] args) =>
        //    Host.CreateDefaultBuilder(args)
        //        .ConfigureWebHostDefaults(webBuilder =>
        //        {
        //            webBuilder.UseStartup<Startup>();
        //        });
    }
}