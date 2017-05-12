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

            double minx = bp.Entities.Select((e)=>e.Position.X).Min();
            double miny = bp.Entities.Select((e)=>e.Position.Y).Min();

            foreach(var entity in bp.Entities)
            {
                entity.Position.X -= minx;
                entity.Position.Y -= miny;
            }

            Clipboard.SetText(bp.ExportBlueprintString());
        }
    }
}
