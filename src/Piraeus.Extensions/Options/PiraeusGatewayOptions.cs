using System;
using Microsoft.Extensions.Logging;
using Piraeus.Configuration;

namespace Piraeus.Extensions.Options
{
    public enum OrleansStorageType
    {
        Memory = 0,

        AzureStorage = 1,

        Redis = 2
    }

    public class PiraeusGatewayOptions
    {
        public PiraeusGatewayOptions()
        {
        }

        public PiraeusGatewayOptions(OrleansConfig config)
        {
            Dockerized = config.Dockerized;
            ClusterId = config.ClusterId;
            ServiceId = config.ServiceId;
            DataConnectionString = config.DataConnectionString;
            AppInsightKey = config.InstrumentationKey;
            LoggerTypes = config.GetLoggerTypes();
            LoggingLevel = Enum.Parse<LogLevel>(config.LogLevel);
            SetStorageType();
        }

        public string AppInsightKey
        {
            get; set;
        }

        public string ClusterId
        {
            get; set;
        }

        public string DataConnectionString
        {
            get; set;
        }

        public bool Dockerized
        {
            get; set;
        }

        public LoggerType LoggerTypes
        {
            get; set;
        }

        public LogLevel LoggingLevel
        {
            get; set;
        }

        public string ServiceId
        {
            get; set;
        }

        public OrleansStorageType StorageType
        {
            get; set;
        }

        private OrleansStorageType SetStorageType()
        {
            if (string.IsNullOrEmpty(DataConnectionString))
            {
                return default;
            }

            string cs = DataConnectionString.ToLowerInvariant();
            if (cs.Contains(":6380") || cs.Contains(":6379"))
            {
                return OrleansStorageType.Redis;
            }

            if (cs.Contains("defaultendpointsprotocol=") && cs.Contains("accountname=") && cs.Contains("accountkey="))
            {
                return OrleansStorageType.AzureStorage;
            }

            throw new ArgumentException("Invalid connection string");
        }
    }
}