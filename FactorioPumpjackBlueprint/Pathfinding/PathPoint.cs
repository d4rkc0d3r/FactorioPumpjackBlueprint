using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactorioPumpjackBlueprint.Pathfinding
{
    enum PathPointStatus { None, Open, Closed }

    class PathPoint : Coord
    {

        public PathPoint Parent { get; set; }
        public float Distance { get; set; }
        public float PredictedDistance { get { return Heuristic + Distance; } }
        public float Heuristic { get; private set; }
        public PathPointStatus Status { get; set; }

        public PathPoint(int x, int y, float heuristic) : base(x, y)
        {
            Heuristic = heuristic;
            Status = PathPointStatus.None;
            Distance = 0;
            Parent = null;
        }
    }
}
