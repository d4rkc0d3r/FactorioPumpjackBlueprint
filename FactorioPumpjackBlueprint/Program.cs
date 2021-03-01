﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using FactorioPumpjackBlueprint.Pathfinding;
using Newtonsoft.Json;
using Ionic.Zlib;
using System.IO;

namespace FactorioPumpjackBlueprint
{
    class Program
    {
        static Blueprint LayPipes(Blueprint bp, bool useSpeed3, int minPumpjacksPerBeacon)
        {
            Profiler.StartSection("copyBP");
            bp = bp.DeepCopy();
            Profiler.EndSection();

            Profiler.StartSection("initializeLayPipes");
            double minx = bp.Entities.Select(e => e.Position.X).Min();
            double miny = bp.Entities.Select(e => e.Position.Y).Min();

            foreach (var entity in bp.Entities)
            {
                entity.Position.Sub(minx - 6, miny - 6);
            }

            double maxx = bp.Entities.Select(e => e.Position.X).Max();
            double maxy = bp.Entities.Select(e => e.Position.Y).Max();

            int width = (int)Math.Ceiling(maxx) + 7;
            int height = (int)Math.Ceiling(maxy) + 7;

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
            Profiler.EndSection();

            #region Add pump jack output pipes
            Profiler.StartSection("pumpjackOutputPipes");
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
                        case Direction.North: p.Add(1, -2); break;
                        case Direction.East: p.Add(2, -1); break;
                        case Direction.South: p.Add(-1, 2); break;
                        case Direction.West: p.Add(-2, 1); break;
                        default: p = null; break;
                    }
                    if (p == null || occupant[(int)p.X, (int)p.Y] == null)
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
                bp.AddEntity(e);
            }
            toAdd.Clear();
            Profiler.EndSection();
            #endregion

            #region Create pump jack distance map with Dijkstra
            Profiler.StartSection("distanceMap");
            IList<Entity> pipes = bp.Entities.Where(e => string.Equals(e.Name, "pipe")).ToList();
            IDictionary<int, int[,]> distanceMap = new Dictionary<int, int[,]>();
            var openQueue = new RingQueue(Math.Max(width, height) * 5);
            foreach (Entity pipe in pipes)
            {
                int[,] distanceField = new int[width, height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        distanceField[x, y] = -1;
                    }
                }
                openQueue.Enqueue(((int)pipe.Position.X) | (((int)pipe.Position.Y) << 16));
                while (openQueue.Count > 0)
                {
                    int x = openQueue.Dequeue();
                    int y = x >> 16;
                    x &= 0xFFFF;
                    if (occupant[x, y] != null || distanceField[x, y] != -1)
                        continue;
                    int smallest = int.MaxValue;
                    int x2 = x;
                    int y2 = y - 1;
                    if (y2 >= 0)
                    {
                        int d = distanceField[x2, y2];
                        if (d != -1)
                        {
                            smallest = Math.Min(smallest, d);
                        }
                        else
                        {
                            openQueue.Enqueue(x2 | (y2 << 16));
                        }
                    }
                    x2 = x - 1;
                    y2 = y;
                    if (x2 >= 0)
                    {
                        int d = distanceField[x2, y2];
                        if (d != -1)
                        {
                            smallest = Math.Min(smallest, d);
                        }
                        else
                        {
                            openQueue.Enqueue(x2 | (y2 << 16));
                        }
                    }
                    x2 = x + 1;
                    y2 = y;
                    if (x2 < width)
                    {
                        int d = distanceField[x2, y2];
                        if (d != -1)
                        {
                            smallest = Math.Min(smallest, d);
                        }
                        else
                        {
                            openQueue.Enqueue(x2 | (y2 << 16));
                        }
                    }
                    x2 = x;
                    y2 = y + 1;
                    if (y2 < height)
                    {
                        int d = distanceField[x2, y2];
                        if (d != -1)
                        {
                            smallest = Math.Min(smallest, d);
                        }
                        else
                        {
                            openQueue.Enqueue(x2 | (y2 << 16));
                        }
                    }
                    distanceField[x, y] = (smallest == int.MaxValue) ? 0 : smallest + 1;
                }
                distanceMap.Add(pipe.EntityNumber, distanceField);
            }
            Profiler.EndSection();
            #endregion

            #region Create pipe MST between pump jacks
            Profiler.StartSection("pumpjackPipeMST");
            var newPipeSet = new HashSet<Coord>();
            var allPipeIds = pipes.Select(p => p.EntityNumber).ToList();
            var allEdges = new List<Edge>();
            var mstEdges = new HashSet<Edge>();
            var mstIds = new HashSet<int>();
            for (int i = 0; i < pipes.Count; i++)
            {
                var p = pipes[i];
                var d = distanceMap[p.EntityNumber];
                for (int j = 0; j < pipes.Count; j++)
                {
                    if (i == j)
                        continue;
                    var c = new Coord(pipes[j].Position);
                    var distance = d[c.X, c.Y];
                    if (distance == -1)
                        continue;
                    allEdges.Add(new Edge() { Start = p.EntityNumber, End = pipes[j].EntityNumber, Distance = distance });
                }
            }
            allEdges = allEdges.OrderBy(e => e.Distance).ToList();
            Edge edge = allEdges.First();
            mstIds.Add(edge.Start);
            mstIds.Add(edge.End);
            mstEdges.Add(edge);

            bool disconnected = false;

            while (mstIds.Count < allPipeIds.Count)
            {
                edge = allEdges.FirstOrDefault(e => mstIds.Contains(e.Start) ^ mstIds.Contains(e.End));
                if (edge == null)
                {
                    disconnected = true;
                    break;
                }
                mstIds.Add(edge.Start);
                mstIds.Add(edge.End);
                mstEdges.Add(edge);
            }
            var directNeighborOffsets = new Coord[] {
                new Coord(-1,0),
                new Coord(0,-1),
                new Coord(1,0),
                new Coord(0,1)
            };
            var idToPipeMap = pipes.ToDictionary(p => p.EntityNumber);
            foreach (var mstEdge in mstEdges)
            {
                Entity pipe1 = idToPipeMap[mstEdge.Start];
                Entity pipe2 = idToPipeMap[mstEdge.End];
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
                    foreach (var offset in directNeighborOffsets)
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
            foreach (var pipe in pipes)
            {
                newPipeSet.Remove(new Coord(pipe.Position));
            }
            Profiler.EndSection();
            #endregion

            #region Make underground pipes
            Profiler.StartSection("ugPipes");
            var allPipes = new HashSet<Coord>();
            allPipes.UnionWith(pipes.Select(e => new Coord(e.Position)));
            allPipes.UnionWith(newPipeSet);
            
            var ugPipes = ReplaceStraightPipeWithUnderground(newPipeSet, 1, allPipes);

            foreach (var ugPipe in ugPipes)
            {
                bp.AddEntity(ugPipe);
            }
            foreach (var p in newPipeSet)
            {
                bp.CreateEntity("pipe", p.X, p.Y);
            }
            foreach (var entity in bp.Entities)
            {
                occupant[(int)entity.Position.X, (int)entity.Position.Y] = entity;
            }
            Profiler.EndSection();
            #endregion

            if (useSpeed3)
            {
                foreach (var pumpjack in bp.Entities.Where(e => e.Name.Equals("pumpjack")))
                {
                    pumpjack.Items = null;
                    pumpjack.AddItem("speed-module-3", 2);
                }
            }

            double oilFlow = bp.Entities.Select(e => e.Name.Equals("pumpjack") ? (useSpeed3 ? 2 : 1) : 0).Sum();
            int pipeCount = bp.Entities.Count(e => e.Name.Contains("pipe"));

            #region Place beacons
            Profiler.StartSection("beacons");
            if (minPumpjacksPerBeacon > 0)
            {
                const int BEACON_RANGE_RADIUS = 5; // from beacon center to pumpjack center
                Coord[] beaconBBOffsets = new Coord[] {
                    new Coord(-1, -1),
                    new Coord(-1, 0),
                    new Coord(-1, 1),
                    new Coord(0, -1),
                    new Coord(0, 0),
                    new Coord(0, 1),
                    new Coord(1, -1),
                    new Coord(1, 0),
                    new Coord(1, 1)
                };
                int[,] affectedPumpjacks = new int[width, height];
                int[,] pumpjackGrid = new int[width, height];
                foreach (var pos in bp.Entities.Where(e => e.Name.Equals("pumpjack")).Select(e => e.Position))
                {
                    pumpjackGrid[(int)pos.X, (int)pos.Y] = 1;
                }
                int maxAffectedPumpjacks = 0;
                for (int y1 = 1; y1 < height - 1; y1++)
                {
                    for (int x1 = 1; x1 < width - 1; x1++)
                    {
                        if (beaconBBOffsets.Any(o => occupant[x1 + o.X, y1 + o.Y] != null))
                            continue;
                        for (int y2 = -BEACON_RANGE_RADIUS; y2 <= BEACON_RANGE_RADIUS; y2++)
                        {
                            for (int x2 = -BEACON_RANGE_RADIUS; x2 <= BEACON_RANGE_RADIUS; x2++)
                            {
                                int x = x1 + x2;
                                int y = y1 + y2;
                                if (x >= 0 && y >= 0 && x < width && y < height)
                                    affectedPumpjacks[x1, y1] += pumpjackGrid[x, y];
                            }
                        }
                        if (affectedPumpjacks[x1, y1] > maxAffectedPumpjacks)
                            maxAffectedPumpjacks = affectedPumpjacks[x1, y1];
                    }
                }
                for (int i = minPumpjacksPerBeacon; i >= minPumpjacksPerBeacon; i--)
                {
                    for (int y = 1; y < height - 1; y++)
                    {
                        for (int x = 1; x < width - 1; x++)
                        {
                            if (affectedPumpjacks[x, y] >= i)
                            {
                                var beacon = bp.CreateEntity("beacon", x, y);
                                beacon.AddItem("speed-module-3", 2);
                                oilFlow += affectedPumpjacks[x, y] / 2.0;
                                for (int y2 = -2; y2 <= 2; y2++)
                                {
                                    for (int x2 = -2; x2 <= 2; x2++)
                                    {
                                        if (x + x2 < 0 || y + y2 < 0 || x + x2 >= width || y + y2 >= height)
                                            continue;
                                        affectedPumpjacks[x + x2, y + y2] = 0;
                                    }
                                }
                                foreach (var o in beaconBBOffsets)
                                {
                                    occupant[x + o.X, y + o.Y] = beacon;
                                }
                                x += 2;
                            }
                        }
                    }
                }
            }
            Profiler.EndSection();
            #endregion

            bp.extraData = new { PipeCount = pipeCount, Fitness = (oilFlow * 100 - pipeCount) * ((disconnected) ? 0.5 : 1.0), OilProduction = oilFlow };
            bp.Name = bp.Entities.Count(e => e.Name.Equals("pumpjack")) + " pumpjack outpost | " + oilFlow + " oil flow";
            bp.NormalizePositions();

            return bp;
        }

        static void PlacePowerPoles(Blueprint bp)
        {
            Profiler.StartSection("initializePower");
            double minx = bp.Entities.Select(e => e.Position.X).Min();
            double miny = bp.Entities.Select(e => e.Position.Y).Min();

            foreach (var entity in bp.Entities)
            {
                entity.Position.Sub(minx - 6, miny - 6);
            }

            double maxx = bp.Entities.Select(e => e.Position.X).Max();
            double maxy = bp.Entities.Select(e => e.Position.Y).Max();

            int width = (int)Math.Ceiling(maxx) + 7;
            int height = (int)Math.Ceiling(maxy) + 7;

            Entity[,] occupant = new Entity[width, height];

            foreach (var entity in bp.Entities)
            {
                int x = (int)entity.Position.X;
                int y = (int)entity.Position.Y;
                occupant[x, y] = entity;
                if (entity.Name.Equals("pumpjack") || entity.Name.Equals("beacon"))
                {
                    occupant[x - 1, y - 1] = entity;
                    occupant[x - 1, y] = entity;
                    occupant[x - 1, y + 1] = entity;
                    occupant[x, y - 1] = entity;
                    occupant[x, y + 1] = entity;
                    occupant[x + 1, y - 1] = entity;
                    occupant[x + 1, y] = entity;
                    occupant[x + 1, y + 1] = entity;
                }
            }
            Profiler.EndSection();
            Profiler.StartSection("power1");
            var unpoweredEntityMap = bp.Entities.Where(e => e.Name.Equals("pumpjack") || e.Name.Equals("beacon")).ToDictionary(e => new Coord(e.Position));
            var powerPoles = new List<Entity>();
            while (unpoweredEntityMap.Count > 0)
            {
                const int POWER_POLE_REACH_RADIUS = 4;
                double highestPowerCount = 0;
                Position center = new Position(width / 2.0, height / 2.0);
                double centerBiasDivider = 1 + Math.Sqrt(Math.Pow(center.X, 2) + Math.Pow(center.Y, 2));
                Coord bestPosition = new Coord(0, 0);
                IList<Coord> bestPoweredEntities = new List<Coord>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (occupant[x, y] != null)
                        {
                            continue;
                        }
                        IList<Coord> poweredEntities = new List<Coord>();
                        double sum = 0;
                        for (int y2 = -POWER_POLE_REACH_RADIUS; y2 <= POWER_POLE_REACH_RADIUS; y2++)
                        {
                            for (int x2 = -POWER_POLE_REACH_RADIUS; x2 <= POWER_POLE_REACH_RADIUS; x2++)
                            {
                                Coord c = new Coord(x + x2, y + y2);
                                if (unpoweredEntityMap.ContainsKey(c))
                                {
                                    sum++;
                                    poweredEntities.Add(c);
                                }
                            }
                        }
                        sum -= Math.Sqrt(Math.Pow(center.X - x, 2) + Math.Pow(center.Y - y, 2)) / centerBiasDivider;
                        if (sum > highestPowerCount)
                        {
                            bestPosition = new Coord(x, y);
                            highestPowerCount = sum;
                            bestPoweredEntities = poweredEntities;
                        }
                    }
                }
                if (highestPowerCount <= 0)
                {
                    break;
                }
                Entity powerPole = bp.CreateEntity("medium-electric-pole", bestPosition.X, bestPosition.Y);
                powerPoles.Add(powerPole);
                occupant[bestPosition.X, bestPosition.Y] = powerPole;
                foreach (Coord c in bestPoweredEntities)
                {
                    unpoweredEntityMap.Remove(c);
                }
            }
            Profiler.EndSection();
            if (powerPoles.Count > 1)
            {
                Profiler.StartSection("powerMST");
                var allPoleIds = powerPoles.Select(p => p.EntityNumber).ToList();
                var allEdges = new List<Edge>();
                var mstEdges = new HashSet<Edge>();
                var mstIds = new HashSet<int>();
                for (int i = 0; i < powerPoles.Count; i++)
                {
                    var p1 = powerPoles[i];
                    for (int j = 0; j < powerPoles.Count; j++)
                    {
                        if (i == j)
                            continue;
                        var p2 = powerPoles[j];
                        var distance = p1.Position.DistanceTo(p2.Position);
                        allEdges.Add(new Edge() { Start = p1.EntityNumber, End = powerPoles[j].EntityNumber, Distance = distance });
                    }
                }
                allEdges = allEdges.OrderBy(e => e.Distance).ToList();
                var edge = allEdges.First();
                mstIds.Add(edge.Start);
                mstIds.Add(edge.End);
                mstEdges.Add(edge);
                while (mstIds.Count < allPoleIds.Count)
                {
                    edge = allEdges.First(e => (!mstIds.Contains(e.Start) && mstIds.Contains(e.End)) || (mstIds.Contains(e.Start) && !mstIds.Contains(e.End)));
                    mstIds.Add(edge.Start);
                    mstIds.Add(edge.End);
                    mstEdges.Add(edge);
                }
                Profiler.EndSection();
                Profiler.StartSection("powerAStar");
                var idToPoleMap = powerPoles.ToDictionary(p => p.EntityNumber);
                AStar astar = new AStar(occupant, 9);
                foreach (var mstEdge in mstEdges)
                {
                    Entity pole1 = idToPoleMap[mstEdge.Start];
                    Entity pole2 = idToPoleMap[mstEdge.End];
                    if (mstEdge.Distance <= 9)
                    {
                        pole1.AddNeighbour(pole2);
                        pole2.AddNeighbour(pole1);
                        continue;
                    }
                    Coord start = new Coord(pole1.Position);
                    Coord end = new Coord(pole2.Position);
                    Entity lastPole = pole1;
                    List<Coord> e = astar.FindPath(start, end).ToList();
                    for(int i = 1; i < e.Count - 1; i++)
                    {
                        Entity pole = bp.CreateEntity("medium-electric-pole", e[i].X, e[i].Y);
                        lastPole.AddNeighbour(pole);
                        pole.AddNeighbour(lastPole);
                        lastPole = pole;
                    }
                    lastPole.AddNeighbour(pole2);
                    pole2.AddNeighbour(lastPole);
                }
            }
            bp.NormalizePositions();
            Profiler.EndSection();
        }

        static HashSet<Entity> ReplaceStraightPipeWithUnderground(HashSet<Coord> pipesToReplace, int minGapToReplace = 1, HashSet<Coord> allPipes = null)
        {
            const int MAX_UNDERGROUND_PIPE_DISTANCE = 11;

            if (allPipes == null)
            {
                allPipes = pipesToReplace;
            }

            var undergroundPipes = new HashSet<Entity>();
            var ugPipeEndPointsY = new HashSet<Coord>();
            var ugPipeEndPointsX = new HashSet<Coord>();

            int minx = allPipes.Select(c => c.X).Min();
            int miny = allPipes.Select(c => c.Y).Min();
            int maxx = allPipes.Select(c => c.X).Max();
            int maxy = allPipes.Select(c => c.Y).Max();

            for (int x = minx; x <= maxx; x++)
            {
                for (int y = miny; y <= maxy; y++)
                {
                    int yend = y;
                    while (yend - y < MAX_UNDERGROUND_PIPE_DISTANCE &&
                        pipesToReplace.Contains(new Coord(x, yend)) &&
                        !allPipes.Contains(new Coord(x - 1, yend)) &&
                        !allPipes.Contains(new Coord(x + 1, yend)))
                    {
                        yend++;
                    }
                    yend--;
                    if (yend - y > minGapToReplace)
                    {
                        undergroundPipes.Add(new Entity("pipe-to-ground", x, y, Direction.North));
                        undergroundPipes.Add(new Entity("pipe-to-ground", x, yend, Direction.South));
                        ugPipeEndPointsY.Add(new Coord(x, yend));
                        y = yend;
                    }
                }
            }

            for (int y = miny; y <= maxy; y++)
            {
                for (int x = minx; x <= maxx; x++)
                {
                    int xend = x;
                    while (xend - x < MAX_UNDERGROUND_PIPE_DISTANCE &&
                        pipesToReplace.Contains(new Coord(xend, y)) &&
                        !allPipes.Contains(new Coord(xend, y - 1)) &&
                        !allPipes.Contains(new Coord(xend, y + 1)))
                    {
                        xend++;
                    }
                    xend--;
                    if (xend - x > minGapToReplace)
                    {
                        undergroundPipes.Add(new Entity("pipe-to-ground", x, y, Direction.West));
                        undergroundPipes.Add(new Entity("pipe-to-ground", xend, y, Direction.East));
                        ugPipeEndPointsX.Add(new Coord(xend, y));
                        x = xend;
                    }
                }
            }

            foreach (Entity ugPipe in undergroundPipes)
            {
                if (ugPipe.Direction == Direction.North)
                {
                    int x = (int)ugPipe.Position.X;
                    int y = (int)ugPipe.Position.Y;
                    for(; y <= maxy && !ugPipeEndPointsY.Contains(new Coord(x, y)); y++)
                    {
                        pipesToReplace.Remove(new Coord(x, y));
                    }
                    pipesToReplace.Remove(new Coord(x, y));
                }
                else if (ugPipe.Direction == Direction.West)
                {
                    int x = (int)ugPipe.Position.X;
                    int y = (int)ugPipe.Position.Y;
                    for (; x <= maxx && !ugPipeEndPointsX.Contains(new Coord(x, y)); x++)
                    {
                        pipesToReplace.Remove(new Coord(x, y));
                    }
                    pipesToReplace.Remove(new Coord(x, y));
                }
            }

            return undergroundPipes;
        }

        static void PrintHelp()
        {
            Console.WriteLine("FactorioPumpjackBlueprint.exe [-s3] [-b] [-i=\\d+] [-seed=\\d+] [-json]");
            Console.WriteLine("                 The blueprint string gets read from clipboard");
            Console.WriteLine("-s(peed)?3       Puts speed3 modules in the pumjacks");
            Console.WriteLine("-b(eacon)?(=\\d)? Places speed3 beacons and activates -s3, defaults to min 2 affacted pumpjacks per beacon");
            Console.WriteLine("-i=\\d+           Specifies number of mutations for optimization, defaults to 100");
            Console.WriteLine("-seed=\\d+        Specifies random number seed to get deterministic results");
            Console.WriteLine("-json            Displays decoded blueprint json instead of running the pumpjack field code");
        }

        [STAThreadAttribute]
        static void Main(string[] args)
        {
            bool useSpeed3 = false;
            int minPumpjacksPerBeacon = 0;
            int maxIterationsWithoutImprovement = 100;
            bool showTimeUsedPercent = false;
            Random rng = new Random();

            foreach (string arg in args.Select(s => s.ToLowerInvariant()))
            {
                if (Regex.IsMatch(arg, "-s(peed)?3"))
                {
                    useSpeed3 = true;
                }
                else if (Regex.IsMatch(arg, "-b(eacon)?(=\\d+)?"))
                {
                    if (arg.Contains("="))
                    {
                        minPumpjacksPerBeacon = int.Parse(arg.Substring(arg.IndexOf('=') + 1));
                        if (minPumpjacksPerBeacon < 1)
                            minPumpjacksPerBeacon = 1;
                    }
                    else
                    {
                        minPumpjacksPerBeacon = 2;
                    }
                    useSpeed3 = true;
                }
                else if (Regex.IsMatch(arg, "-(h(elp)?|\\?)"))
                {
                    PrintHelp();
                    return;
                }
                else if (Regex.IsMatch(arg, "-i=\\d+"))
                {
                    maxIterationsWithoutImprovement = int.Parse(arg.Substring(3));
                }
                else if (Regex.IsMatch(arg, "-seed=\\d+"))
                {
                    rng = new Random(int.Parse(arg.Substring(6)));
                }
                else if (Regex.IsMatch(arg, "-t(ime)?"))
                {
                    showTimeUsedPercent = true;
                }
                else if (Regex.IsMatch(arg, "-json"))
                {
                    string blueprintJSON = null;
                    try
                    {
                        using (var msi = new MemoryStream(Convert.FromBase64String(Clipboard.GetText().Substring(1))))
                        {
                            using (var mso = new MemoryStream())
                            {
                                using (var gs = new ZlibStream(msi, CompressionMode.Decompress))
                                {
                                    gs.CopyTo(mso);
                                }
                                blueprintJSON = Encoding.UTF8.GetString(mso.ToArray());
                            }
                        }
                    }
                    catch (FormatException)
                    {
                        return;
                    }
                    blueprintJSON = blueprintJSON.Substring(13, blueprintJSON.Length - 14);
                    string s = JsonConvert.SerializeObject(JsonConvert.DeserializeObject(blueprintJSON), Formatting.Indented);
                    Console.WriteLine(s);
                    return;
                }
                else
                {
                    Console.WriteLine("Unknown option: " + arg);
                }
            }

            Profiler.StartSection("importBlueprint");
            Blueprint originalBp = Blueprint.ImportBlueprintString(Clipboard.GetText());
            Profiler.EndSection();

            if (originalBp == null)
            {
                Console.WriteLine("Could not load blueprint");
                return;
            }

            int iterationsWithoutImprovement = 0;
            Profiler.StartSection("copyBP");
            Blueprint bestBp = originalBp.DeepCopy();
            Profiler.EndSection();
            Blueprint bestFinishedBp = LayPipes(originalBp, useSpeed3, minPumpjacksPerBeacon);
            double bestFitness = bestFinishedBp.extraData.Fitness;
            Console.WriteLine("Found layout with " + bestFinishedBp.extraData.PipeCount + " pipes and " +
                bestFinishedBp.extraData.OilProduction + " oil flow.");

            while (++iterationsWithoutImprovement <= maxIterationsWithoutImprovement)
            {
                Profiler.StartSection("copyBP");
                Blueprint bp = bestBp.DeepCopy();
                Profiler.EndSection();
                Profiler.StartSection("randomizeRotation");
                var pumpjackIdMap = bp.Entities.Where(e => "pumpjack".Equals(e.Name)).ToDictionary(e => e.EntityNumber);
                var pumpjackIds = bp.Entities.Where(e => "pumpjack".Equals(e.Name)).Select(p => p.EntityNumber).ToList();
                int randomizeAmount = rng.Next(5);
                for (int i = 0; i <= randomizeAmount; i++)
                {
                    int id = rng.Next(pumpjackIds.Count);
                    int direction = rng.Next(4) * 2;
                    pumpjackIdMap[pumpjackIds[id]].Direction = direction;
                }
                Profiler.EndSection();
                var test = LayPipes(bp, useSpeed3, minPumpjacksPerBeacon);
                double testFitness = test.extraData.Fitness;

                if (testFitness > bestFitness)
                {
                    bestFitness = testFitness;
                    bestBp = bp;
                    bestFinishedBp = test;
                    Console.WriteLine("Found layout with " + bestFinishedBp.extraData.PipeCount + " pipes and " +
                        bestFinishedBp.extraData.OilProduction + " oil flow after " + iterationsWithoutImprovement + " iterations.");
                    iterationsWithoutImprovement = 0;
                }
            }

            PlacePowerPoles(bestFinishedBp);

            if(showTimeUsedPercent)
                Profiler.PrintTimeUsedPercent();
            
            Clipboard.SetText(bestFinishedBp.ExportBlueprintString());
        }
    }
}
