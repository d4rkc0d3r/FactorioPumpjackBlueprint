using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactorioPumpjackBlueprint.Pathfinding
{
    class AStar
    {
        private Entity[,] entityMap;
        private int width;
        private int height;

        private Dictionary<Coord, PathPoint> points;
        private List<Tuple<Coord, float>> offsetPoints;
        private Coord target;

        private void Initialize(Coord target)
        {
            this.points = new Dictionary<Coord, PathPoint>();
            this.target = new Coord(target);
        }

        private PathPoint PathPoint(int x, int y)
        {
            return PathPoint(new Coord(x, y));
        }

        private PathPoint PathPoint(Coord c)
        {
            PathPoint o;
            if (!points.TryGetValue(c, out o))
            {
                o = new PathPoint(c.X, c.Y, Heuristic(c, target));
                points.Add(c, o);
            }
            return o;
        }

        private static float Heuristic(Coord a, Coord b)
        {
            var x = a.X - b.X;
            var y = a.Y - b.Y;
            //return Math.Abs(x) + Math.Abs(y); // Manhattan
            return (float)Math.Sqrt(x * x + y * y); // Euclid
        }

        private bool IsOccupied(Coord c)
        {
            return IsOccupied(c.X, c.Y);
        }

        private bool IsOccupied(int x, int y)
        {
            if (target.X == x && target.Y == y)
                return false;
            if (x < 0 || y < 0 || x >= width || y >= height)
                return true;
            return entityMap[x, y] != null;
        }

        public AStar(Entity[,] entityMap, double maxDistancePerStep)
        {
            this.entityMap = entityMap;
            this.width = entityMap.GetLength(0);
            this.height = entityMap.GetLength(1);
            int r = (int)Math.Ceiling(maxDistancePerStep);
            offsetPoints = new List<Tuple<Coord, float>>();
            for (int y = -r; y <= r; y++) for(int x = -r; x <= r; x++)
            {
                float distance = (float)Math.Sqrt(x * x + y * y);
                if (distance > maxDistancePerStep || distance < 1)
                    continue;
                distance = (float)Math.Abs(maxDistancePerStep / 2 - distance);
                offsetPoints.Add(new Tuple<Coord,float>(new Coord(x, y), (float)(distance + maxDistancePerStep)));
            }
            offsetPoints = offsetPoints.OrderByDescending(t => t.Item2).ToList();
        }

        /*
         * Returns a list, which contains the optimal path between a start and end Coord
         */
        public IEnumerable<Coord> FindPath(Coord start, Coord end)
        {
            Initialize(end);

            var s = PathPoint(start);
            var openHeap = new PathPointMinHeap { s };

            // if there are any unanalyzed nodes, process them
            while (openHeap.Count > 0)
            {
                // get the node with the lowest estimated distance to end node
                var current = openHeap.Pop();
                current.Status = PathPointStatus.None;

                // if finish
                if (target.Equals(current))
                {
                    // generate the found path
                    return ReconstructPath(current);
                }

                // process each valid node around the current node
                foreach (var pair in GetNeighborNodes(current))
                {
                    var neighbor = pair.Item1;
                    var tempCurrentDistance = current.Distance + pair.Item2;

                    if (neighbor.Status == PathPointStatus.Closed)
                    {
                        continue;
                    }

                    if (neighbor.Status == PathPointStatus.Open && tempCurrentDistance >= neighbor.Distance)
                    {
                        continue;
                    }

                    neighbor.Parent = current;
                    neighbor.Distance = tempCurrentDistance;

                    if (neighbor.Status != PathPointStatus.Open)
                    {
                        openHeap.Add(neighbor);
                        neighbor.Status = PathPointStatus.Open;
                    }
                }

                current.Status = PathPointStatus.Closed;
            }

            return null;
        }

        // Returns a list of accessible neighbors and their distance cost
        private IEnumerable<Tuple<PathPoint, float>> GetNeighborNodes(Coord node)
        {
            var nodes = new List<Tuple<PathPoint, float>>();

            foreach (var tuple in offsetPoints)
            {
                int x = node.X + tuple.Item1.X;
                int y = node.Y + tuple.Item1.Y;
                if (!IsOccupied(x, y))
                    nodes.Add(new Tuple<PathPoint, float>(PathPoint(x, y), tuple.Item2));
            }

            return nodes;
        }

        // Reconstructs Path from List of Coord
        // return The shortest path from the start to the destination node.</returns>
        private IEnumerable<Coord> ReconstructPath(PathPoint node)
        {
            var path = new LinkedList<Coord>();
            while (node != null)
            {
                path.AddFirst(node);
                node = node.Parent;
            }
            return path;
        }
    }
}
