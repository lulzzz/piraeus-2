using System;

namespace SkunkLab.Channels
{
    public class ObserverEventArgs : EventArgs
    {
        public ObserverEventArgs(Uri resourceUri, string contentType, byte[] message)
        {
            ResourceUri = resourceUri;
            ContentType = contentType;
            Message = message;
        }

        public string ContentType
        {
            get; set;
        }

        public byte[] Message
        {
            get; set;
        }

        public Uri ResourceUri
        {
            get; set;
        }
    }
}