﻿using Newtonsoft.Json;
using System;

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
        public OrleansConfig()
        {
        }

        [JsonProperty("clusterId")]
        public string ClusterId { get; set; }

        [JsonProperty("dataConnectionString")]
        public string DataConnectionString { get; set; }

        [JsonProperty("dockerized")]
        public bool Dockerized { get; set; }  //true for docker deployments; otherwise local deployment

        //orleans cluster id

        [JsonProperty("instrumentationKey")]
        public string InstrumentationKey { get; set; }

        [JsonProperty("loggerTypes")]
        public string LoggerTypes { get; set; } = "Console;Debug";

        [JsonProperty("logLevel")]
        public string LogLevel { get; set; } = "Warning";

        [JsonProperty("serviceId")]
        public string ServiceId { get; set; } //orleans service id

        //either azure storage connection string or redis connection string

        [JsonProperty("servicePointFactor")]
        public int ServicePointFactor { get; set; } = 24; //service point factor, e.g., 24 associated with Azure storage

        //any of console, debug, file, appinsights, or none.

        //one of warning, error, information, critical, verbose

        //required when loggertypes as appinsights; otherwise omit

        public LoggerType GetLoggerTypes()
        {
            if (string.IsNullOrEmpty(LoggerTypes))
            {
                return default(LoggerType);
            }

            string loggerTypes = LoggerTypes.Replace(";", ",");
            return Enum.Parse<LoggerType>(loggerTypes, true);
        }
    }
}