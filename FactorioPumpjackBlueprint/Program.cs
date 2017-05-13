using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FactorioPumpjackBlueprint
{
    class Program
    {
        [STAThreadAttribute]
        static void Main(string[] args)
        {
            Blueprint bp = Blueprint.ImportBlueprintString(Clipboard.GetText());

            if (bp == null)
            {
                Console.WriteLine("Could not load blueprint");
                return;
            }

            double minx = bp.Entities.Select(e => e.Position.X).Min();
            double miny = bp.Entities.Select(e => e.Position.Y).Min();

            foreach (var entity in bp.Entities)
            {
                entity.Position.Sub(minx - 5, miny - 5);
            }

            double maxx = bp.Entities.Select(e => e.Position.X).Max();
            double maxy = bp.Entities.Select(e => e.Position.Y).Max();

            int width = (int)Math.Ceiling(maxx) + 6;
            int height = (int)Math.Ceiling(maxy) + 6;

            Entity[,] occupant = new Entity[width, height];

            foreach (var entity in bp.Entities)
            {
                if (!entity.Name.Equals("pumpjack"))
                    continue;
                int x = (int)entity.Position.X;
                int y = (int)entity.Position.Y;
                occupant[x - 1, y - 1] = entity;
                occupant[x - 1, y] = entity;
                occupant[x - 1, y + 1] = entity;
                occupant[x, y - 1] = entity;
                occupant[x, y] = entity;
                occupant[x, y + 1] = entity;
                occupant[x + 1, y - 1] = entity;
                occupant[x + 1, y] = entity;
                occupant[x + 1, y + 1] = entity;
            }

            List<Entity> toAdd = new List<Entity>();
            foreach (var entity in bp.Entities)
            {
                if (!entity.Name.Equals("pumpjack"))
                    continue;
                Position p = null;
                int tries = 0;
                do
                {
                    p = new Position(entity.Position);
                    switch (entity.Direction)
                    {
                        case 0: p.Add(1, -2); break;
                        case 2: p.Add(2, -1); break;
                        case 4: p.Add(-1, 2); break;
                        case 6: p.Add(-2, 1); break;
                        default: p = null; break;
                    }
                    if(p == null || occupant[(int)p.X, (int)p.Y] == null)
                    {
                        break;
                    }
                    p = null;
                    entity.Direction = (entity.Direction + 2) % 8;
                } while (++tries < 4);
                if (p == null)
                    continue;
                toAdd.Add(new Entity("pipe", p));
            }
            foreach (var e in toAdd)
            {
                bp.Entities.Add(e);
            }
            toAdd.Clear();

            IList<Entity> pipes = bp.Entities.Where(e => string.Equals(e.Name, "pipe")).ToList();
            IDictionary<int, int[,]> distanceMap = new Dictionary<int, int[,]>();
            var offsets = new Coord[] {
                new Coord(-1,0),
                new Coord(0,-1),
                new Coord(1,0),
                new Coord(0,1)
            };
            foreach (Entity pipe in pipes)
            {
                Console.WriteLine("Calculate distance field for entity number " + pipe.EntityNumber);
                int[,] distanceField = new int[width, height];
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        distanceField[x, y] = -1;
                    }
                }
                Queue<Coord> openQueue = new Queue<Coord>();
                openQueue.Enqueue(new Coord(pipe.Position));
                while(openQueue.Count > 0)
                {
                    Coord c = openQueue.Dequeue();
                    if (c.X < 0 || c.Y < 0 || c.X >= width || c.Y >= height || occupant[c.X, c.Y] != null || distanceField[c.X, c.Y] != -1)
                        continue;
                    int smallest = int.MaxValue;
                    foreach (var offset in offsets)
                    {
                        Coord t = c.Add(offset);
                        if (t.X < 0 || t.Y < 0 || t.X >= width || t.Y >= height)
                            continue;
                        int d = distanceField[t.X, t.Y];
                        if (d != -1)
                        {
                            smallest = Math.Min(smallest, d);
                        }
                        else
                        {
                            openQueue.Enqueue(t);
                        }
                    }
                    distanceField[c.X, c.Y] = (smallest == int.MaxValue) ? 0 : smallest + 1;
                }
                distanceMap.Add(pipe.EntityNumber, distanceField);
            }

            var newPipeSet = new HashSet<Coord>();
            for (int i = 0; i < pipes.Count - 1; i++)
            {
                Entity pipe1 = pipes[i];
                Entity pipe2 = pipes[i + 1];
                Coord start = new Coord(pipe1.Position);
                Coord end = new Coord(pipe2.Position);
                var distanceField = distanceMap[pipe1.EntityNumber];
                if (distanceField[end.X, end.Y] == -1)
                    continue;
                Coord c = end;
                while (true)
                {
                    int smallest = int.MaxValue;
                    Coord next = null;
                    foreach (var offset in offsets)
                    {
                        Coord t = c.Add(offset);
                        if (t.X < 0 || t.Y < 0 || t.X >= width || t.Y >= height)
                            continue;
                        int d = distanceField[t.X, t.Y];
                        if (d != -1 && d < smallest)
                        {
                            smallest = d;
                            next = t;
                        }
                    }
                    if (next == null || next.Equals(start))
                    {
                        break;
                    }
                    c = next;
                    newPipeSet.Add(c);
                }
            }
            {
                bp.Entities.Add(new Entity("pipe", p.X, p.Y));
            }

            bp.NormalizePositions();
            Clipboard.SetText(bp.ExportBlueprintString());
        }
    }
}
