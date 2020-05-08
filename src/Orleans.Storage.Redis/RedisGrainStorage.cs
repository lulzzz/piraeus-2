using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans.Runtime;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Binary;

namespace Orleans.Storage.Redis
{
    public class RedisGrainStorage : IGrainStorage, ILifecycleParticipant<ISiloLifecycle>
    {
        private readonly ILogger logger;

        private readonly string name;

        private readonly RedisStorageOptions options;

        private ConnectionMultiplexer connection;

        private IDatabase database;

        private BinarySerializer serializer;

        public RedisGrainStorage(string name, RedisStorageOptions options, ILogger logger = null)
        {
            this.name = name;
            this.options = options;
            this.logger = logger;
        }

        public async Task ClearStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            string key = grainReference.ToKeyString();

            try
            {
                await database.KeyDeleteAsync(key);
                logger.LogDebug($"Redis grain state deleted {key}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed clear state for key '{key}'.");
            }
        }

        public void Participate(ISiloLifecycle lifecycle)
        {
            try
            {
                lifecycle.Subscribe(OptionFormattingUtilities.Name<RedisGrainStorage>(name), options.InitStage, Init);
                logger.LogInformation($"Lifecycle started for {name}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Redis grain storage failed participate in lifecycle.");
            }
        }

        public async Task ReadStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            string key = grainReference.ToKeyString();

            try
            {
                var val = await database.StringGetAsync(key);

                if (!val.IsNull)
                {
                    if (options.Serializer == SerializerType.Json)
                    {
                        grainState.State = JsonConvert.DeserializeObject(val);
                    }
                    else
                    {
                        grainState.State = serializer.Deserialize(val);
                    }
                }

                logger.LogDebug($"Redis grain state read {key}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed read state for key '{key}'.");
            }
        }

        public async Task WriteStateAsync(string grainType, GrainReference grainReference, IGrainState grainState)
        {
            var key = grainReference.ToKeyString();

            try
            {
                var state = grainState.State;

                if (options.Serializer == SerializerType.Json)
                {
                    var payload = JsonConvert.SerializeObject(state);
                    await database.StringSetAsync(key, payload);
                }
                else
                {
                    var payload = serializer.Serialize(state);
                    await database.StringSetAsync(key, payload);
                }

                logger.LogDebug($"Redis grain state wrote {key}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Failed write state for key '{key}'");
            }
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

            return null;
        }

        private ConfigurationOptions GetRedisConfiguration()
        {
            ConfigurationOptions configOptions;
            if (!string.IsNullOrEmpty(options.ConnectionString))
            {
                configOptions = ConfigurationOptions.Parse(options.ConnectionString);
            }
            else
            {
                configOptions = new ConfigurationOptions
                {
                    ConnectRetry = options.ConnectRetry ?? 4,
                    DefaultDatabase = options.DatabaseNo ?? 1,
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

        private async Task Init(CancellationToken ct)
        {
            if (options.Serializer == SerializerType.BinaryFormatter)
            {
                serializer = new BinarySerializer();
            }

            if (connection != null && !connection.IsConnected)
            {
                await connection.CloseAsync();
            }

            ConfigurationOptions configOptions = GetRedisConfiguration();

            connection = await ConnectionMultiplexer.ConnectAsync(configOptions);
            database = connection.GetDatabase();
            logger.LogInformation("Redis grain state connection open.");
        }
    }
}