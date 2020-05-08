using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkunkLab.Protocols.Coap
{
    internal class OptionBuilder
    {
        private readonly SortedList<int, CoapOption> list;

        private readonly Dictionary<int, int> optionDict;

        public OptionBuilder()
        {
            list = new SortedList<int, CoapOption>();
            optionDict = new Dictionary<int, int>();
        }

        public OptionBuilder(IEnumerable<CoapOption> options)
        {
            _ = options ?? throw new ArgumentNullException(nameof(options));

            optionDict = new Dictionary<int, int>();

            list = new SortedList<int, CoapOption>();
            IEnumerator<CoapOption> en = options.GetEnumerator();
            while (en.MoveNext())
            {
                int typeInt = (int)en.Current.Type;

                if (optionDict.ContainsKey(typeInt))
                {
                    optionDict[typeInt] = optionDict[typeInt] + 1;
                }
                else
                {
                    optionDict.Add(typeInt, 0);
                }

                list.Add((int)en.Current.Type * 1000 + optionDict[typeInt], en.Current);
            }
        }

        public void Append(CoapOption option)
        {
            int typeInt = (int)option.Type;

            if (optionDict.ContainsKey(typeInt))
            {
                optionDict[typeInt] = optionDict[typeInt]++;
            }
            else
            {
                optionDict.Add(typeInt, 0);
            }

            list.Add((int)option.Type * 1000 + optionDict[typeInt], option);
        }

        public byte[] Encode()
        {
            byte[] options = null;
            int previous = 0;

            using (MemoryStream stream = new MemoryStream())
            {
                KeyValuePair<int, CoapOption>[] kvps = list.ToArray();
                foreach (KeyValuePair<int, CoapOption> kvp in kvps)
                {
                    byte[] encodedOption = kvp.Value.Encode(previous);

                    stream.Write(encodedOption, 0, encodedOption.Length);
                    previous = (int)kvp.Value.Type;
                }

                stream.Position = 0;
                options = stream.ToArray();
            }

            return options;
        }
    }
}