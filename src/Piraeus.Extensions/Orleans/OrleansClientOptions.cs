using System;
using Microsoft.Extensions.Logging;

namespace Piraeus.Extensions.Orleans
{
    [Serializable]
    public class OrleansClientOptions
    {
        public string DataConnectionString
        {
            get; set;
        }

        public string[] Loggers
        {
            get; set;
        }

        public LogLevel LoggingLevel
        {
            get; set;
        }

        public MembershipProviderType StorageType
        {
            get; set;
        }
    }
}