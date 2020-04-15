using System;
using System.Collections.Generic;

namespace Piraeus.GrainInterfaces
{
    [Serializable]
    public class SigmaAlgebraChainState
    {
        public long Id { get; set; }
        public List<string> Container { get; set; }
    }
}
