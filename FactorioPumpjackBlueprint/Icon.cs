using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FactorioPumpjackBlueprint
{
    class Icon
    {
        [JsonProperty("signal")]
        public Signal Signal { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        public Icon DeepCopy()
        {
            return new Icon() { Signal = Signal.DeepCopy(), Index = Index};
        }
    }
}
