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

        private int nextEntityId = 1;

        public Entity CreateEntity(string name, double x, double y, int direction = 0)
        {
            var entity = new Entity(name, x, y, direction);
            AddEntity(entity);
            return entity;
        }

        public void AddEntity(Entity entity)
        {
            entity.EntityNumber = nextEntityId++;
            if (Entities == null)
                Entities = new List<Entity>();
            Entities.Add(entity);
        }

        public Blueprint DeepCopy()
        {
            var bp = new Blueprint();
            if (Icons != null)
            {
                bp.Icons = new List<Icon>();
                foreach (var icon in Icons)
                {
                    bp.Icons.Add(icon.DeepCopy());
                }
            }
            if (Entities != null)
            {
                bp.Entities = new List<Entity>();
                foreach (var entity in Entities)
                {
                    bp.Entities.Add(entity.DeepCopy());
                }
            }
            bp.Version = Version;
            bp.nextEntityId = nextEntityId;
            bp.Name = Name;
            bp.Version = Version;
            return bp;
        }

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
            Blueprint bp = JsonConvert.DeserializeObject<Blueprint>(blueprintJSON);
            if (bp.Entities != null)
                bp.nextEntityId = bp.Entities.Select(e => e.EntityNumber).Max() + 1;
            return bp;
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
