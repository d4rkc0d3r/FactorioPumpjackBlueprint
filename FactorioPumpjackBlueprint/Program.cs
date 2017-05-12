using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
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

            dynamic bp = JsonConvert.DeserializeObject(blueprintJSON);

            foreach (var entity in bp.blueprint.entities)
            {
                entity.position.x += 10;
            }

            string newbpString = "0" + Convert.ToBase64String(Zip(JsonConvert.SerializeObject(bp, Formatting.None)));

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
