using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FactorioPumpjackBlueprint
{
    class Position
    {
        [JsonProperty("x")]
        public double X { get; set; }

        [JsonProperty("y")]
        public double Y { get; set; }

        public Position Set(double x, double y)
        {
            X = x;
            Y = y;
            return this;
        }

        public Position Sub(Position p)
        {
            X -= p.X;
            Y -= p.Y;
            return this;
        }

        public Position Sub(double x, double y)
        {
            X -= x;
            Y -= y;
            return this;
        }

        public Position Add(Position p)
        {
            X += p.X;
            Y += p.Y;
            return this;
        }

        public Position Add(double x, double y)
        {
            X += x;
            Y += y;
            return this;
        }

        public Position()
        {

        }

        public Position(Position p)
        {
            X = p.X;
            Y = p.Y;
        }

        public Position(double x, double y)
        {
            X = x;
            Y = y;
        }
    }
}
