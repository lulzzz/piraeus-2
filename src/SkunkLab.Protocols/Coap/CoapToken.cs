using System;

namespace SkunkLab.Protocols.Coap
{
    public class CoapToken
    {
        private static readonly Random ran;

        static CoapToken()
        {
            ran = new Random();
        }

        public CoapToken()
        {
        }

        public CoapToken(byte[] tokenBytes)
        {
            TokenBytes = tokenBytes;
        }

        public byte[] TokenBytes
        {
            get; set;
        }

        public string TokenString => Convert.ToBase64String(TokenBytes);

        public static CoapToken Create()
        {
            byte[] buffer = new byte[8];
            ran.NextBytes(buffer);
            return new CoapToken(buffer);
        }
    }
}