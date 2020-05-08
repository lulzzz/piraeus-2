using System;
using Newtonsoft.Json;

namespace Piraeus.Configuration
{
    [Flags]
    public enum LoggerType
    {
        None = 0,

        Console = 1,

        Debug = 2,

        AppInsights = 4,

        File = 8
    }

    [Serializable]
    [JsonObject]
    public class OrleansConfig
    {
        [JsonProperty("clusterId")]
        public string ClusterId
        {
            get; set;
        }

        [JsonProperty("dataConnectionString")]
        public string DataConnectionString
        {
            get; set;
        }

        [JsonProperty("dockerized")]
        public bool Dockerized
        {
            get; set;
        }

        [JsonProperty("instrumentationKey")]
        public string InstrumentationKey
        {
            get; set;
        }

        [JsonProperty("loggerTypes")]
        public string LoggerTypes { get; set; } = "Console;Debug";

        [JsonProperty("logLevel")]
        public string LogLevel { get; set; } = "Warning";

        [JsonProperty("serviceId")]
        public string ServiceId
        {
            get; set;
        }

        [JsonProperty("servicePointFactor")]
        public int ServicePointFactor { get; set; } = 24;

        public LoggerType GetLoggerTypes()
        {
            if (string.IsNullOrEmpty(LoggerTypes))
            {
                return default;
            }

            string loggerTypes = LoggerTypes.Replace(";", ",");
            return Enum.Parse<LoggerType>(loggerTypes, true);
        }
    }
}