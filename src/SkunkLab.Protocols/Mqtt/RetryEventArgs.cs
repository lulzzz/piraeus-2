using System;

namespace SkunkLab.Protocols.Mqtt
{
    public class RetryEventArgs : EventArgs
    {
        public RetryEventArgs(MqttMessage message)
        {
            Message = message;
        }

        public MqttMessage Message
        {
            get; internal set;
        }
    }
}