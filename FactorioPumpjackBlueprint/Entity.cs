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
        private int entityNumber;

        [JsonProperty("entity_number")]
        public int EntityNumber { get {return entityNumber;} set{entityNumber = value; MaxEntityNumber = Math.Max(MaxEntityNumber, entityNumber);} }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("position")]
        public Position Position { get; set; }

        [JsonProperty("direction")]
        public int Direction { get; set; }

        [JsonProperty("items")]
        public IList<Item> Items { get; set; }

        public Entity()
        {

        }

        public Entity(string name)
        {
            EntityNumber = MaxEntityNumber + 1;
            Position = new Position(0, 0);
            Name = name;
        }

        public Entity(string name, double x, double y, int direction = 0)
        {
            EntityNumber = MaxEntityNumber + 1;
            Position = new Position(x, y);
            Name = name;
            Direction = direction;
        }

        public Entity(string name, Position p, int direction = 0)
        {
            EntityNumber = MaxEntityNumber + 1;
            Position = new Position(p);
            Name = name;
            Direction = direction;
        }

        public static int MaxEntityNumber = 0;
    }
}
