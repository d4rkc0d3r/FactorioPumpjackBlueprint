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
            double sum = (double)timeUsed.Values.Sum();
            foreach (var sectionName in timeUsed.Keys)
            {
                double p = Math.Round(timeUsed[sectionName] / sum * 10000) / 100;
                Console.WriteLine("Section " + sectionName + " took " + p + "% of time in the measured sections.");
            }
        }
    }
}
