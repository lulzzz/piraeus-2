using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Piraeus.Core.Messaging
{
    [Serializable]
    [JsonObject]
    public class ListContinuationToken
    {
        public ListContinuationToken()
        {
        }

        public ListContinuationToken(int index, int quantity, int pageSize)
        {
            Index = index;
            Quantity = quantity;
            PageSize = pageSize;
        }

        public ListContinuationToken(int index, int quantity, int pageSize, string filter)
        {
            Index = index;
            Quantity = quantity;
            PageSize = pageSize;
            Filter = filter;
        }

        public ListContinuationToken(int index, int quantity, int pageSize, string filter, List<string> items)
        {
            Index = index;
            Quantity = quantity;
            PageSize = pageSize;
            Filter = filter;
            Items = items;
        }

        public ListContinuationToken(int index, int quantity, int pageSize, List<string> items)
        {
            Index = index;
            Quantity = quantity;
            PageSize = pageSize;
            Items = items;
        }

        [JsonProperty("filter")]
        public string Filter
        {
            get; set;
        }

        [JsonProperty("index")]
        public int Index
        {
            get; set;
        }

        [JsonProperty("items")]
        public List<string> Items
        {
            get; set;
        }

        [JsonProperty("pageSize")]
        public int PageSize
        {
            get; set;
        }

        [JsonProperty("quantity")]
        public int Quantity
        {
            get; set;
        }
    }
}