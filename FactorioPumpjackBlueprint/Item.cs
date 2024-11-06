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
        public class TargetStack
        {
            [JsonProperty("inventory")]
            public int Inventory { get; set; }

            [JsonProperty("stack")]
            public int Stack { get; set; }

            public TargetStack DeepCopy()
            {
                var item = new TargetStack();
                item.Inventory = Inventory;
                item.Stack = Stack;
                return item;
            }
        }

        public class InInventory
        {
            [JsonProperty("in_inventory")]
            public IList<TargetStack> Content { get; set; }

            public InInventory DeepCopy()
            {
                var copy = new InInventory();
                copy.Content = Content.Select(x => x.DeepCopy()).ToList();
                return copy;
            }
        }

        public class Id
        {
            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("quality")]
            public string Quality { get; set; }

            public Id DeepCopy()
            {
                var id = new Id();
                id.Name = Name;
                id.Quality = Quality;
                return id;
            }
        }

        [JsonProperty("id")]
        public Id ItemId { get; set; }

        [JsonProperty("items")]
        public InInventory Items { get; set; }

        private Item() { }

        public Item(string name, string quality, int count, int inventory)
        {
            ItemId = new Id() { Name = name, Quality = quality };
            Items = new InInventory() { Content = new List<TargetStack>() };
            for (int i = 0; i < count; i++)
            {
                Items.Content.Add(new TargetStack() { Inventory = inventory, Stack = i });
            }
        }

        public Item DeepCopy()
        {
            var item = new Item();
            item.ItemId = ItemId.DeepCopy();
            item.Items = Items?.DeepCopy();
            return item;
        }
    }
}
