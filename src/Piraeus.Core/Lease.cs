﻿using System;

namespace Piraeus.Core
{
    [Serializable]
    public class Lease
    {
        public Lease()
        {
        }

        public TimeSpan Duration { get; set; }
        public string Key { get; set; }
    }
}