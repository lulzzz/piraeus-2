using System;
using System.Collections.Generic;

namespace Piraeus.GrainInterfaces
{
    [Serializable]
    public class SigmaAlgebraChainState
    {
        public List<string> Container
        {
            get; set;
        }

        public long Id
        {
            get; set;
        }
    }
}