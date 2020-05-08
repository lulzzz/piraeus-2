namespace Orleans.Storage.Redis
{
    public enum SerializerType
    {
        BinaryFormatter,

        Json
    }

    public class RedisStorageOptions
    {
        public const int DEFAULT_INIT_STAGE = ServiceLifecycleStage.ApplicationServices;

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

        public int InitStage { get; set; } = DEFAULT_INIT_STAGE;

        public bool IsLocalDocker { get; set; } = false;

        public string Password
        {
            get; set;
        }

        public int? ResponseTimeout
        {
            get; set;
        }

        public SerializerType Serializer
        {
            get; set;
        }

        public int? SyncTimeout
        {
            get; set;
        }
    }
}