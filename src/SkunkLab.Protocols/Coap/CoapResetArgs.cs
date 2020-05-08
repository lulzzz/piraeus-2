using System;

namespace SkunkLab.Protocols.Coap
{
    [Serializable]
    public class CoapResetArgs : EventArgs
    {
        public CoapResetArgs(ushort messageId, string internalMessageId, CodeType code)
        {
            MessageId = messageId;
            InternalMessageId = internalMessageId;
            Code = code;
        }

        public CodeType Code
        {
            get; internal set;
        }

        public string InternalMessageId
        {
            get; internal set;
        }

        public ushort MessageId
        {
            get; internal set;
        }
    }
}