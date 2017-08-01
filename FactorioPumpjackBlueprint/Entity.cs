using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FactorioPumpjackBlueprint
{
    class Entity
    {
        [JsonProperty("entity_number")]
        public int EntityNumber { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("direction")]
        public int Direction { get; set; }

        [JsonProperty("items")]
        public JObject Items { get; set; }

        public void AddItem(string name, int count = 1)
        {
            if (Items == null)
                Items = new JObject();
            Items[name] = count;
        }

        public Entity()
        {

        }

        public Entity DeepCopy()
        {
            var e = new Entity();
            e.Name = Name;
            e.Position = Position.DeepCopy();
            e.EntityNumber = EntityNumber;
            e.Direction = Direction;
            if (Items != null)
            {
                e.Items = (JObject)Items.DeepClone();
            }
            return e;
        }

        public Entity(string name)
        {
            Position = new Position(0, 0);
            Name = name;
        }

        public Entity(string name, double x, double y, int direction = 0)
        {
            Position = new Position(x, y);
            Name = name;
            Direction = direction;
        }

        public Entity(string name, Position p, int direction = 0)
        {
            Position = new Position(p);
            Name = name;
            Direction = direction;
        }
    }
}
