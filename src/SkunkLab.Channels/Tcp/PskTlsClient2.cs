using System;
using Org.BouncyCastle.Crypto.Tls;

namespace SkunkLab.Channels.Tcp
{
    public class PskTlsClient2 : PskTlsClient
    {
        public PskTlsClient2(TlsCipherFactory cipherFactory, TlsPskIdentity pskIdentity)
            : base(cipherFactory, pskIdentity)
        {
        }

        public PskTlsClient2(TlsPskIdentity pskIdentity)
            : base(pskIdentity)
        {
        }

        public bool IsHandshakeComplete
        {
            get; set;
        }

        public override ProtocolVersion MinimumVersion => ProtocolVersion.TLSv12;

        public override void NotifyAlertRaised(byte alertLevel, byte alertDescription, string message, Exception cause)
        {
            Console.WriteLine(message);
            base.NotifyAlertRaised(alertLevel, alertDescription, message, cause);
        }

        public override void NotifyHandshakeComplete()
        {
            IsHandshakeComplete = true;
            base.NotifyHandshakeComplete();
        }
    }
}