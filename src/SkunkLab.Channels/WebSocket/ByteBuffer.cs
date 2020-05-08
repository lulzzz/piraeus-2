using System;
using System.Collections.Generic;
using System.Text;

namespace SkunkLab.Channels.WebSocket
{
    internal sealed class ByteBuffer
    {
        private readonly int maxLength;

        private readonly List<byte[]> segments = new List<byte[]>();

        private int currentLength;

        public ByteBuffer(int maxLength)
        {
            this.maxLength = maxLength;
        }

        public void Append(byte[] segment)
        {
            currentLength += segment.Length;
            if (currentLength > maxLength)
            {
                throw new InvalidOperationException("Length exceeded.");
            }

            segments.Add(segment);
        }

        public byte[] GetByteArray()
        {
            byte[] dst = new byte[currentLength];
            int dstOffset = 0;
            for (int i = 0; i < segments.Count; i++)
            {
                byte[] src = segments[i];
                Buffer.BlockCopy(src, 0, dst, dstOffset, src.Length);
                dstOffset += src.Length;
            }

            return dst;
        }

        public string GetString()
        {
            StringBuilder builder = new StringBuilder();
            Decoder decoder = Encoding.UTF8.GetDecoder();
            for (int i = 0; i < segments.Count; i++)
            {
                bool flush = i == segments.Count - 1;
                byte[] bytes = segments[i];
                char[] chars = new char[decoder.GetCharCount(bytes, 0, bytes.Length, flush)];
                int charCount = decoder.GetChars(bytes, 0, bytes.Length, chars, 0, flush);
                builder.Append(chars, 0, charCount);
            }

            return builder.ToString();
        }
    }
}