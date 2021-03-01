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

        [JsonProperty("neighbours")]
        public IList<int> Neighbours { get; set; }

        public void AddItem(string name, int count = 1)
        {
            if (Items == null)
                Items = new JObject();
            Items[name] = count;
        }

        public void AddNeighbour(int entityId)
        {
            if (Neighbours == null)
                Neighbours = new List<int>();
            Neighbours.Add(entityId);
        }

        public void AddNeighbour(Entity entity)
        {
            AddNeighbour(entity.EntityNumber);
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
            if(Neighbours != null)
            {
                e.Neighbours = new List<int>(Neighbours);
            }
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
