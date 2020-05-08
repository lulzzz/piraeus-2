using System;

namespace SkunkLab.Protocols.Coap
{
    [Serializable]
    public class CoapAckArgs : EventArgs
    {
        public CoapAckArgs(ushort messageId, string internalMessageId, byte[] token, CodeType code, string faultMessage)
        {
            MessageId = messageId;
            InternalMessageId = internalMessageId;
            Token = token;
            Code = code;
            FaultMessage = faultMessage;
        }

        public CoapAckArgs(ushort messageId, string internalMessageId, byte[] token, CodeType code, string contentType,
            byte[] responseMessage)
        {
            MessageId = messageId;
            InternalMessageId = internalMessageId;
            Token = token;
            Code = code;
            ContentType = contentType;
            ResponseMessage = responseMessage;
        }

        public CodeType Code
        {
            get; internal set;
        }

        public string ContentType
        {
            get; internal set;
        }

        public string FaultMessage
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

        public byte[] ResponseMessage
        {
            get; internal set;
        }

        public byte[] Token
        {
            get; internal set;
        }
    }
}