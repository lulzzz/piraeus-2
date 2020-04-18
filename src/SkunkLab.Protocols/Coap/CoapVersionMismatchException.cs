using System;
using System.Runtime.Serialization;

namespace SkunkLab.Protocols.Coap
{
    [Serializable]
    public class CoapVersionMismatchException : Exception
    {
        public CoapVersionMismatchException()
        {
        }

        public CoapVersionMismatchException(string message)
            : base(message)
        {
        }

        public CoapVersionMismatchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected CoapVersionMismatchException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}