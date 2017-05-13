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
                entity.Position.Sub(minx - 2, miny - 2);
            }

            double maxx = bp.Entities.Select(e => e.Position.X).Max();
            double maxy = bp.Entities.Select(e => e.Position.Y).Max();

            int width = (int)Math.Ceiling(maxx) + 2;
            int height = (int)Math.Ceiling(maxy) + 2;

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

            bp.NormalizePositions();

            Clipboard.SetText(bp.ExportBlueprintString());
        }
    }
}
