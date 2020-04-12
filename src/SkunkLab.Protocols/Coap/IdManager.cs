using System.Collections.Generic;

namespace SkunkLab.Protocols.Coap
{
    public class IdManager
    {
        private readonly HashSet<ushort> container;

        private ushort currentId;

        public IdManager()
        {
            container = new HashSet<ushort>();
        }

        private ushort NewId()
        {
            currentId++;

            while (container.Contains(currentId))
            {
                currentId = currentId == ushort.MaxValue ? (ushort)1 : currentId;
            }

            container.Add(currentId);

            return currentId;
        }

        private void Remove(ushort id)
        {
            container.Remove(id);
        }
    }
}