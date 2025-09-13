using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaPresentacion.Impresion
{
    public class TicketRenderer
    {
        private const int Cols = 42; // 80mm típico

        public static string Render(ComandaTicket t, string etiquetaDestino = null)
        {
            var sb = new StringBuilder();

            sb.AppendLine(Center("COMANDA", true));
            if (!string.IsNullOrWhiteSpace(etiquetaDestino))
                sb.AppendLine(Center("DESTINO: " + etiquetaDestino, true));
            if (!string.IsNullOrWhiteSpace(t.Ambiente))
                sb.AppendLine(Center(t.Ambiente.ToUpperInvariant(), false));

            sb.AppendLine(new string('-', Cols));
            sb.AppendLine(Line("Fecha: " + t.FechaHora.ToString("dd/MM/yyyy"), "Hora: " + t.FechaHora.ToString("HH:mm")));
            sb.AppendLine(Line("Pedido: " + t.NroPedido, "Mesa: " + t.Mesa));
            var pers = t.NroPersonas.HasValue ? t.NroPersonas.Value.ToString() : "-";
            sb.AppendLine(Line("Personas: " + pers, "Vend: " + t.Vendedor));
            sb.AppendLine(new string('-', Cols));

            foreach (var l in t.Lineas)
            {
                var cab = TrimPadLeft(l.Cantidad) + " x " + l.NombreProducto;
                foreach (var row in Wrap(cab, Cols)) sb.AppendLine(row);

                if (!string.IsNullOrWhiteSpace(l.Notas))
                {
                    foreach (var row in Wrap("  * " + l.Notas, Cols)) sb.AppendLine(row);
                }

                sb.AppendLine();
            }

            sb.AppendLine(new string('-', Cols));
            sb.AppendLine(Center("¡EN PREPARACIÓN!", true));
            sb.AppendLine("\n\n");
            return sb.ToString();
        }

        // helpers
        private static string Center(string s, bool upper)
        {
            if (s == null) s = "";
            if (s.Length > Cols) s = s.Substring(0, Cols);
            int pad = (Cols - s.Length) / 2;
            if (pad < 0) pad = 0;
            var line = new string(' ', pad) + s;
            return upper ? line.ToUpperInvariant() : line;
        }

        private static string Line(string left, string right)
        {
            if (left == null) left = "";
            if (right == null) right = "";
            int sp = Cols - left.Length - right.Length;
            if (sp < 1) sp = 1;
            return left + new string(' ', sp) + right;
        }

        private static IEnumerable<string> Wrap(string text, int width)
        {
            if (text == null) text = "";
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var line = "";
            foreach (var w in words)
            {
                if (line.Length == 0)
                {
                    line = w;
                }
                else if (line.Length + 1 + w.Length > width)
                {
                    yield return line;
                    line = w;
                }
                else
                {
                    line = line + " " + w;
                }
            }
            if (!string.IsNullOrEmpty(line)) yield return line;
        }

        private static string TrimPadLeft(decimal cant)
        {
            string s = (cant % 1 == 0) ? ((int)cant).ToString() : cant.ToString("0.##");
            return s.PadLeft(3);
        }
    }
}
