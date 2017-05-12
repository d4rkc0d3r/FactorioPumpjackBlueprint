using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FactorioPumpjackBlueprint
{
    class Blueprint
    {
        [JsonProperty("icons")]
        public IList<Icon> Icons { get; set; }

        [JsonProperty("entities")]
        public IList<Entity> Entities { get; set; }

        [JsonProperty("item")]
        public string Item { get { return "blueprint"; } set { return; } }

        [JsonProperty("version")]
        public double Version { get; set; }
    }
}
