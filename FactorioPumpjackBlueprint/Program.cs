using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ionic.Zlib;

namespace FactorioPumpjackBlueprint
{
    class Program
    {
        [STAThreadAttribute]
        static void Main(string[] args)
        {
            string bpString = Clipboard.GetText();
            if (String.IsNullOrEmpty(bpString))
            {
                Console.WriteLine("Empty clipboard");
                Console.ReadKey();
                return;
            }

            string blueprintJSON = null;
            try
            {
                blueprintJSON = Unzip(Convert.FromBase64String(bpString.Substring(1)));
            }
            catch(FormatException fe) 
            {
                Console.WriteLine(fe.ToString());
                Console.ReadKey();
            }
            blueprintJSON = blueprintJSON.Substring(13, blueprintJSON.Length - 14);
            Blueprint bp = JsonConvert.DeserializeObject<Blueprint>(blueprintJSON);

            double minx = bp.Entities.Select((e)=>e.Position.X).Min();
            double miny = bp.Entities.Select((e)=>e.Position.Y).Min();

            foreach(var entity in bp.Entities)
            {
                entity.Position.X -= minx;
                entity.Position.Y -= miny;
            }

            //bp.Entities.Clear();
            string bpStringOut = @"{""blueprint"":" + JsonConvert.SerializeObject(bp, Formatting.None) + "}";
            Clipboard.SetText(JsonConvert.SerializeObject(bp, Formatting.Indented));

            Console.ReadKey();

            string newbpString = "0" + Convert.ToBase64String(Zip(@"{""blueprint"":" + JsonConvert.SerializeObject(bp, Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore }) + "}"));

            Clipboard.SetText(newbpString);
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {

                using (var gs = new ZlibStream(mso, CompressionMode.Compress))
                {
                    msi.CopyTo(gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            {
                using (var mso = new MemoryStream())
                {
                    using (var gs = new ZlibStream(msi, CompressionMode.Decompress))
                    {
                        gs.CopyTo(mso);
                    }

                    return Encoding.UTF8.GetString(mso.ToArray());
                }
            }
        }
    }
}
