using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Ionic.Zlib;
using System.IO;

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

        [JsonProperty("label")]
        public string Name { get; set; }

        public dynamic extraData;

        public void NormalizePositions()
        {
            double minx = Entities.Select(e => e.Position.X).Min();
            double miny = Entities.Select(e => e.Position.Y).Min();
            foreach (var entity in Entities)
            {
                entity.Position.Sub(minx, miny);
            }

            double maxx = Entities.Select(e => e.Position.X).Max();
            double maxy = Entities.Select(e => e.Position.Y).Max();
            foreach (var entity in Entities)
            {
                entity.Position.Sub(maxx / 2, maxy / 2);
            }
        }

        public static Blueprint ImportBlueprintString(string bpString)
        {
            string blueprintJSON = null;
            try
            {
                using (var msi = new MemoryStream(Convert.FromBase64String(bpString.Substring(1))))
                {
                    using (var mso = new MemoryStream())
                    {
                        using (var gs = new ZlibStream(msi, CompressionMode.Decompress))
                        {
                            gs.CopyTo(mso);
                        }
                        blueprintJSON = Encoding.UTF8.GetString(mso.ToArray());
                    }
                }
            }
            catch (FormatException)
            {
                return null;
            }
            blueprintJSON = blueprintJSON.Substring(13, blueprintJSON.Length - 14);
            return JsonConvert.DeserializeObject<Blueprint>(blueprintJSON);
        }

        public string ExportBlueprintString()
        {
            var bytes = Encoding.UTF8.GetBytes(@"{""blueprint"":" + JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) + "}");

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {

                using (var gs = new ZlibStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return "0" + Convert.ToBase64String(mso.ToArray());
            }
        }
    }
}
