using System;
using System.Runtime.Serialization;

namespace Capl.Issuance
{
    [Serializable]
    public class IssueModeNotRecognizedException : Exception
    {
        public IssueModeNotRecognizedException()
        {
        }

        public IssueModeNotRecognizedException(string message)
            : base(message)
        {
        }

        public IssueModeNotRecognizedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected IssueModeNotRecognizedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}