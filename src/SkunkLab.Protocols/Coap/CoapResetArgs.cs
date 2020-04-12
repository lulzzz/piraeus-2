namespace SkunkLab.Protocols.Coap
{
    using System;

    [Serializable]
    public class CoapResetArgs : EventArgs
    {
        public CoapResetArgs(ushort messageId, string internalMessageId, CodeType code)
        {
            this.MessageId = messageId;
            this.InternalMessageId = internalMessageId;
            this.Code = code;
        }

        public CodeType Code { get; internal set; }

        public string InternalMessageId { get; internal set; }

        public ushort MessageId { get; internal set; }
    }
}