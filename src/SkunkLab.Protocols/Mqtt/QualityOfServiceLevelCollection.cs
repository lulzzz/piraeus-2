using System.Collections;
using System.Collections.Generic;

namespace SkunkLab.Protocols.Mqtt
{
    public class QualityOfServiceLevelCollection : IList<QualityOfServiceLevelType>
    {
        private readonly List<QualityOfServiceLevelType> items;

        public QualityOfServiceLevelCollection()
        {
            items = new List<QualityOfServiceLevelType>();
        }

        public QualityOfServiceLevelCollection(IEnumerable<QualityOfServiceLevelType> qosLevels)
        {
            items = new List<QualityOfServiceLevelType>(qosLevels);
        }

        public int Count => items.Count;

        public bool IsReadOnly => false;

        public QualityOfServiceLevelType this[int index]
        {
            get => items[index];
            set => items[index] = value;
        }

        public void Add(QualityOfServiceLevelType item)
        {
            items.Add(item);
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool Contains(QualityOfServiceLevelType item)
        {
            return items.Contains(item);
        }

        public void CopyTo(QualityOfServiceLevelType[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<QualityOfServiceLevelType> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(QualityOfServiceLevelType item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, QualityOfServiceLevelType item)
        {
            items.Insert(index, item);
        }

        public bool Remove(QualityOfServiceLevelType item)
        {
            return items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
        }
    }
}