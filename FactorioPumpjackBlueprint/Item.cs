using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FactorioPumpjackBlueprint
{
    class Item
    {
        [JsonProperty("item")]
        public string Name { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        public Item DeepCopy()
        {
            var item = new Item();
            item.Name = Name;
            item.Count = Count;
            return item;
        }
    }
}
