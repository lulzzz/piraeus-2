using System.Collections.Generic;
using System.Security;
using System.Text;
using Org.BouncyCastle.Crypto.Tls;

namespace SkunkLab.Channels.Tcp
{
    public class PskIdentityManager : TlsPskIdentityManager
    {
        private readonly Dictionary<string, byte[]> container;

        private readonly byte[] psk;

        public PskIdentityManager(Dictionary<string, byte[]> psks)
        {
            container = psks;
        }

        public PskIdentityManager(byte[] psk)
        {
            this.psk = psk;
        }

        public byte[] GetHint()
        {
            return null;
        }

        public byte[] GetPsk(byte[] identity)
        {
            string identityString = Encoding.UTF8.GetString(identity);
            if (container.ContainsKey(identityString))
            {
                return container[identityString];
            }

            throw new SecurityException("Identity not found for PSK");
        }
    }
}