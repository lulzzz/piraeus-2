using System;

namespace Orleans.Clustering.Redis
{
    [Serializable]
    public class RedisClusteringOptions
    {
        public string ConnectionString
        {
            get; set;
        }

        public int? ConnectRetry
        {
            get; set;
        }

        public int? DatabaseNo
        {
            get; set;
        }

        public string Hostname
        {
            get; set;
        }

        public bool IsLocalDocker { get; set; } = false;

        public string Password
        {
            get; set;
        }

        public int? ResponseTimeout
        {
            get; set;
        }

        public int? SyncTimeout
        {
            get; set;
        }
    }
}