using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactorioPumpjackBlueprint
{
    class Coord
    {
        private readonly int hashCode;

        private readonly int x;
        private readonly int y;

        public int X { get { return x; } }
        public int Y { get { return y; } }

        /// <summary>
        /// Floors the input coordinates
        /// </summary>
        /// <param name="vector"></param>
        public Coord(Position position) : this(position.X, position.Y) { }

        /// <summary>
        /// Floors the input coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public Coord(double x, double y) : this((int)Math.Floor(x), (int)Math.Floor(y)) { }

        public Coord(Coord coord) : this(coord.x, coord.y) { }

        public Coord(int x, int y)
        {
            this.x = x;
            this.y = y;
            hashCode = (x & 0x7FFF) | ((y & 0x7FFF) << 15);
        }

        public Coord Add(Coord c)
        {
            return new Coord(x + c.x, y + c.y);
        }

        public bool Equals(Coord c)
        {
            return c != null && x == c.x && y == c.y;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Coord);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        public override string ToString()
        {
            return "(" + x + "," + y + ")";
        }
    }
}
