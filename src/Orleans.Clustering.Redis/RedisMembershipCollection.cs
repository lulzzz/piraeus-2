using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Orleans.Runtime;

namespace Orleans.Clustering.Redis
{
    [Serializable]
    public class RedisMembershipCollection : IList<RedisMembershipEntry>
    {
        public static readonly TableVersion tableVersion = new TableVersion(0, "0");

        private readonly List<RedisMembershipEntry> list;

        public RedisMembershipCollection()
        {
            list = new List<RedisMembershipEntry>();
        }

        public int Count => list.Count();

        public bool IsReadOnly => false;

        public RedisMembershipEntry this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }

        public void Add(RedisMembershipEntry item)
        {
            if (!HasEntry(item))
            {
                list.Add(item);
            }
        }

        public void Clear()
        {
            list.Clear();
        }

        public bool Contains(RedisMembershipEntry item)
        {
            return list.Contains(item);
        }

        public void CopyTo(RedisMembershipEntry[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public IEnumerator<RedisMembershipEntry> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        public bool HasEntry(RedisMembershipEntry entry)
        {
            var items = list.Where(x =>
                x.DeploymentId == entry.DeploymentId &&
                x.Address.ToParsableString() == entry.Address.ToParsableString());
            return items.Count() > 0;
        }

        public int IndexOf(RedisMembershipEntry item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, RedisMembershipEntry item)
        {
            list.Insert(index, item);
        }

        public bool Remove(RedisMembershipEntry item)
        {
            return list.Remove(item);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        public MembershipTableData ToMembershipTableData()
        {
            try
            {
                List<Tuple<MembershipEntry, string>> data = list.ToArray().Where(x => x != null)
                    .Select(x => x.ToMembershipEntryTuple())
                    .ToList();

                return new MembershipTableData(data, tableVersion);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public MembershipTableData ToMembershipTableData(SiloAddress key)
        {
            List<Tuple<MembershipEntry, string>> data = list.ToArray().Where(x => x != null)
                .Select(x => x.ToMembershipEntryTuple())
                .ToList();

            List<Tuple<MembershipEntry, string>> items =
                data.TakeWhile(x => x.Item1.SiloAddress.ToParsableString() == key.ToParsableString()).ToList();
            return new MembershipTableData(items, tableVersion);
        }

        public bool UpdateIAmAlive(string clusterId, SiloAddress address, DateTime iAmAlivetime)
        {
            bool ret = false;
            string val = iAmAlivetime.ToString();
            var item = list.ToArray().Where(x =>
                x != null && x.DeploymentId == clusterId && x.ParsableAddress == address.ToParsableString()).First();
            if (item != null)
            {
                item.IAmAliveTime = iAmAlivetime;
                ret = true;
            }

            return ret;
        }
    }
}