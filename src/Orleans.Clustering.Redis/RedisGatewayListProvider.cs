using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Messaging;
using Orleans.Runtime;
using Piraeus.Core.Logging;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Binary;

namespace Orleans.Clustering.Redis
{
    public class RedisGatewayListProvider : IGatewayListProvider
    {
        private readonly string clusterId;

        private readonly ConnectionMultiplexer connection;

        private readonly IDatabase database;

        private readonly ILog logger;

        private readonly RedisClusteringOptions options;

        private readonly BinarySerializer serializer;

        public RedisGatewayListProvider(IOptions<RedisClusteringOptions> membershipTableOptions,
            IOptions<ClusterOptions> clusterOptions, ILog logger = null)
        {
            this.logger = logger;
            options = membershipTableOptions.Value;
            ConfigurationOptions configOptions = GetRedisConfiguration();

            clusterId = clusterOptions.Value.ClusterId;
            connection = ConnectionMultiplexer.Connect(configOptions);
            database = connection.GetDatabase();
            serializer = new BinarySerializer();
        }

        public bool IsUpdatable => true;

        public TimeSpan MaxStaleness { get; } = TimeSpan.FromMinutes(1.0);

        public Task<IList<Uri>> GetGateways()
        {
            if (database.KeyExists(clusterId))
            {
                var val = database.StringGet(clusterId);
                RedisMembershipCollection collection = serializer.Deserialize<RedisMembershipCollection>(val);
                try
                {
                    return Task.FromResult<IList<Uri>>(collection
                        .Where(x => x.Status == SiloStatus.Active && x.ProxyPort != 0)
                        .Select(y =>
                        {
                            var endpoint = new IPEndPoint(y.Address.Endpoint.Address, y.ProxyPort);
                            var gatewayAddress = SiloAddress.New(endpoint, y.Address.Generation);
                            return gatewayAddress.ToGatewayUri();
                        }).ToList());
                }
                catch (Exception ex)
                {
                    logger?.LogErrorAsync(ex, "Redis Gateway List Provider - GetGateways").GetAwaiter();
                    return Task.FromResult<IList<Uri>>(null);
                }
            }

            logger?.LogWarningAsync("Redis Gateway List Provider - No Cluster ID exists.").GetAwaiter();
            return Task.FromResult<IList<Uri>>(null);
        }

        public Task InitializeGatewayListProvider()
        {
            return Task.CompletedTask;
        }

        private IPAddress GetIPAddress(string hostname)
        {
            IPHostEntry hostInfo = Dns.GetHostEntry(hostname);
            for (int index = 0; index < hostInfo.AddressList.Length; index++)
            {
                if (hostInfo.AddressList[index].AddressFamily == AddressFamily.InterNetwork)
                {
                    return hostInfo.AddressList[index];
                }
            }

            logger?.LogWarningAsync("Redis Gateway List Provider - No IP address found for host name.").GetAwaiter();
            return null;
        }

        private IPAddress GetIPAddress(EndPoint endpoint)
        {
            if (endpoint is DnsEndPoint dnsEndpoint)
            {
                return GetIPAddress(dnsEndpoint.Host);
            }

            if (endpoint is IPEndPoint ipEndpoint)
            {
                return ipEndpoint.Address;
            }

            logger?.LogWarningAsync("Redis Gateway List Provider - No IP address found for endpoint.").GetAwaiter();
            return null;
        }

        private ConfigurationOptions GetRedisConfiguration()
        {
            ConfigurationOptions configOptions;
            if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                configOptions = ConfigurationOptions.Parse(options.ConnectionString);
                if (options.DatabaseNo == null)
                {
                    configOptions.DefaultDatabase = 2;
                }
            }
            else
            {
                configOptions = new ConfigurationOptions
                {
                    ConnectRetry = options.ConnectRetry ?? 4,
                    DefaultDatabase = options.DatabaseNo ?? 2,
                    SyncTimeout = options.SyncTimeout ?? 10000,
                    EndPoints = {
                        {options.Hostname, 6380}
                    },
                    Password = options.Password
                };
            }

            if (options.IsLocalDocker)
            {
                IPAddress address = GetIPAddress(configOptions.EndPoints[0]);
                EndPoint endpoint = configOptions.EndPoints[0];
                configOptions.EndPoints.Remove(endpoint);
                configOptions.EndPoints.Add(new IPEndPoint(address, 6379));
            }

            return configOptions;
        }
    }
}