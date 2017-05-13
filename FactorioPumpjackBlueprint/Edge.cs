using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactorioPumpjackBlueprint
{
    class Edge
    {
        public int Start { get; set; }
        public int End { get; set; }
        public double Distance { get; set; }

        public override bool Equals(object obj)
        {
            Edge e = obj as Edge;
            return e != null && Start == e.Start && e.End == End;
        }

        public override int GetHashCode()
        {
            return (Start & 0x7FFF) | ((End & 0x7FFF) << 15);
        }
    }
}
