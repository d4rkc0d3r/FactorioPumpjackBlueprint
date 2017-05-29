using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FactorioPumpjackBlueprint
{
    static class Profiler
    {
        private static Dictionary<string, long> timeUsed = new Dictionary<string, long>();
        private static string name = "";
        private static long start;
        private static long unknownStart = DateTime.Now.Ticks;

        public static void StartSection(string name)
        {
            Profiler.name = name;
            start = DateTime.Now.Ticks;
        }

        public static void EndSection()
        {
            long end = DateTime.Now.Ticks;
            long v = 0;
            timeUsed.TryGetValue(name, out v);
            timeUsed[name] = v + end - start;
        }

        public static void PrintTimeUsedPercent()
        {
            long unknownTime = DateTime.Now.Ticks - unknownStart - timeUsed.Values.Sum();
            timeUsed["unknown"] = unknownTime;
            double sum = (double)timeUsed.Values.Sum();
            int maxSectionNameLength = timeUsed.Keys.Select(n => n.Length).Max();
            foreach (var pair in timeUsed.OrderByDescending(p => p.Value))
            {
                double p = Math.Round(pair.Value / sum * 10000) / 100;
                Console.WriteLine("Section {0," + maxSectionNameLength + "} took {1,5}% of time.", pair.Key, p);
            }
        }
    }
}
