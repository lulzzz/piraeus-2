using System.Text;
using Org.BouncyCastle.Crypto.Tls;

namespace SkunkLab.Channels.Tcp
{
    public class SimplePskIdentity : TlsPskIdentity
    {
        private readonly string hint;

        private readonly byte[] psk;

        public SimplePskIdentity(string hint, byte[] psk)
        {
            this.hint = hint;
            this.psk = psk;
        }

        public byte[] GetPsk()
        {
            return psk;
        }

        public byte[] GetPskIdentity()
        {
            return Encoding.UTF8.GetBytes(hint);
        }

        public void NotifyIdentityHint(byte[] psk_identity_hint)
        {
        }

        public void SkipIdentityHint()
        {
        }
    }
}