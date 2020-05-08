using System;
using System.Collections.Generic;
using SkunkLab.Security.Authentication;

namespace SkunkLab.Protocols.Coap
{
    public sealed class CoapConfig
    {
        public CoapConfig(IAuthenticator authenticator, string authority,
            CoapConfigOptions configOptions, bool autoRetry = false,
            double keepAliveSeconds = 180,
            double ackTimeout = 2.0, double ackRandomFactor = 1.5,
            int maxRetransmit = 4, int nstart = 1, double defaultLeisure = 4.0,
            double probingRate = 1.0, double maxLatency = 100.0)
        {
            Authenticator = authenticator;
            Authority = authority;
            ConfigOptions = configOptions;
            AutoRetry = autoRetry;
            KeepAlive = keepAliveSeconds;
            AckTimeout = TimeSpan.FromSeconds(ackTimeout);
            AckRandomFactor = ackRandomFactor;
            MaxRetransmit = maxRetransmit;
            NStart = nstart;
            DefaultLeisure = TimeSpan.FromSeconds(defaultLeisure);
            ProbingRate = probingRate;
            MaxLatency = TimeSpan.FromSeconds(maxLatency);
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

        public string Authority
        {
            get; internal set;
        }

        public bool AutoRetry
        {
            get; internal set;
        }

        public CoapConfigOptions ConfigOptions
        {
            get; internal set;
        }

        public TimeSpan DefaultLeisure
        {
            get; internal set;
        }

        public string DoNotRetainNonconfirmableResponse
        {
            get; internal set;
        }

        public TimeSpan ExchangeLifetime =>
            TimeSpan.FromSeconds(MaxTransmitSpan.TotalSeconds + 2 * MaxLatency.TotalSeconds +
                                 ProcessingDelay.TotalSeconds);

        public string IdentityClaimType
        {
            get; set;
        }

        public List<KeyValuePair<string, string>> Indexes
        {
            get; set;
        }

        public double? KeepAlive
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

        public TimeSpan MaxRTT =>
            TimeSpan.FromSeconds(2.0 * MaxLatency.TotalSeconds + ProcessingDelay.TotalSeconds);

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

        public int NStart
        {
            get; internal set;
        }

        public double ProbingRate
        {
            get; internal set;
        }

        public TimeSpan ProcessingDelay => AckTimeout;
    }
}