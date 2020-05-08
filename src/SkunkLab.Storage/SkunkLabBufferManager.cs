using System.ServiceModel.Channels;
using Microsoft.WindowsAzure.Storage;

namespace SkunkLab.Storage
{
    public class SkunkLabBufferManager : IBufferManager
    {
        private readonly int defaultBufferSize;

        public SkunkLabBufferManager(BufferManager manager, int defaultBufferSize)
        {
            Manager = manager;
            this.defaultBufferSize = defaultBufferSize;
        }

        public BufferManager Manager
        {
            get; internal set;
        }

        public int GetDefaultBufferSize()
        {
            return defaultBufferSize;
        }

        public void ReturnBuffer(byte[] buffer)
        {
            Manager.ReturnBuffer(buffer);
        }

        public byte[] TakeBuffer(int bufferSize)
        {
            return Manager.TakeBuffer(bufferSize);
        }
    }
}