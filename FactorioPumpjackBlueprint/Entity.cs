using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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
        public IList<Item> Items { get; set; }

        public void AddItem(string name, int count = 1)
        {
            if (Items == null)
                Items = new List<Item>();
            Items.Add(new Item() { Name = name, Count = count });
        }

        public Entity()
        {

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
