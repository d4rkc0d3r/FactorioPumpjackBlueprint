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

            double minx = bp.Entities.Select(e => e.Position.X).Min();
            double miny = bp.Entities.Select(e => e.Position.Y).Min();

            foreach (var entity in bp.Entities)
            {
                entity.Position.Sub(minx - 2, miny - 2);
            }

            List<Entity> toAdd = new List<Entity>();

            foreach(var entity in bp.Entities)
            {
                if(!entity.Name.Equals("pumpjack"))
                    continue;
                Position p = new Position(entity.Position);
                switch(entity.Direction)
                {
                    case 0: p.Add(1, -2); break;
                    case 2: p.Add(2, -1); break;
                    case 4: p.Add(-1, 2); break;
                    case 6: p.Add(-2, 1); break;
                    default: p = null; break;
                }
                if (p == null)
                    continue;
                toAdd.Add(new Entity("pipe", p));
            }

            foreach(var e in toAdd)
            {
                bp.Entities.Add(e);
            }

            bp.NormalizePositions();

            Clipboard.SetText(bp.ExportBlueprintString());
        }
    }
}
