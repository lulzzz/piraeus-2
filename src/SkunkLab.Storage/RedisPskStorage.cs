using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace SkunkLab.Storage
{
    public class RedisPskStorage : PskStorageAdapter
    {
        private static RedisPskStorage instance;

        private readonly ConnectionMultiplexer connection;

        private readonly IDatabase database;

        private readonly int? id;

        protected RedisPskStorage(string connectionString)
        {
            ConfigurationOptions configOptions = ConfigurationOptions.Parse(connectionString);
            id = configOptions.DefaultDatabase;
            connection = ConnectionMultiplexer.ConnectAsync(configOptions).GetAwaiter().GetResult();
            database = connection.GetDatabase();
        }

        public static RedisPskStorage CreateSingleton(string connectionString)
        {
            if (instance == null)
            {
                instance = new RedisPskStorage(connectionString);
            }

            return instance;
        }

        public override async Task<string[]> GetKeys()
        {
            EndPoint[] endpoints = connection.GetEndPoints();
            if (endpoints != null && endpoints.Length > 0)
            {
                var server = connection.GetServer(endpoints[0]);
                int dbNum = id ?? 0;
                var keys = server.Keys(dbNum);
                List<string> list = new List<string>();
                foreach (var key in keys)
                    list.Add(key.ToString());

                return await Task.FromResult(list.ToArray());
            }

            return null;
        }

        public override async Task<string> GetSecretAsync(string key)
        {
            return await database.StringGetAsync(key);
        }

        public override async Task RemoveSecretAsync(string key)
        {
            await database.KeyDeleteAsync(key);
        }

        public override async Task SetSecretAsync(string key, string value)
        {
            await database.StringSetAsync(key, value);
        }
    }
}