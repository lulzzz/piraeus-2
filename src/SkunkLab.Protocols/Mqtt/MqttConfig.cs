using System;
using System.Collections.Generic;
using SkunkLab.Security.Authentication;

namespace SkunkLab.Protocols.Mqtt
{
    public sealed class MqttConfig
    {
        public MqttConfig()
        {
        }

        public MqttConfig(double keepAliveSeconds = 180.0, double ackTimeout = 2.0, double ackRandomFactor = 1.5,
            int maxRetransmit = 4, double maxLatency = 100.0, IAuthenticator authenticator = null,
            string identityClaimType = null, List<KeyValuePair<string, string>> indexes = null)
        {
            KeepAliveSeconds = keepAliveSeconds;
            AckTimeout = TimeSpan.FromSeconds(ackTimeout);
            AckRandomFactor = ackRandomFactor;
            MaxRetransmit = maxRetransmit;
            MaxLatency = TimeSpan.FromSeconds(maxLatency);
            Authenticator = authenticator;
            IdentityClaimType = identityClaimType;
            Indexes = indexes;
        }

        public double AckRandomFactor
        {
            get; internal set;
        }

        public TimeSpan AckTimeout
        {
            get; internal set;
        }

        public IAuthenticator Authenticator
        {
            get; set;
        }

        public TimeSpan ExchangeLifetime =>
            TimeSpan.FromSeconds(MaxTransmitSpan.TotalSeconds + 2 * MaxLatency.TotalSeconds + AckTimeout.TotalSeconds);

        public string IdentityClaimType
        {
            get; set;
        }

        public List<KeyValuePair<string, string>> Indexes
        {
            get; set;
        }

        public double KeepAliveSeconds
        {
            get; internal set;
        }

        public TimeSpan MaxLatency
        {
            get; internal set;
        }

        public int MaxRetransmit
        {
            get; internal set;
        }

        public TimeSpan MaxTransmitSpan
        {
            get
            {
                double secs = AckTimeout.TotalSeconds * (Math.Pow(2.0, Convert.ToDouble(MaxRetransmit)) - 1) *
                              AckRandomFactor;
                return TimeSpan.FromSeconds(secs);
            }
        }

        public TimeSpan MaxTransmitWait =>
            TimeSpan.FromSeconds(AckTimeout.TotalSeconds * (Math.Pow(2.0, Convert.ToDouble(MaxRetransmit) + 1) - 1) *
                                 AckRandomFactor);

        public TimeSpan NonLifetime =>
            TimeSpan.FromSeconds(MaxTransmitSpan.TotalSeconds + MaxLatency.TotalSeconds);
    }
}