using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CapaPresentacion.Helpers
{
    public static class ComboTagHelper
    {
        public const string Prefix = "[#";
        public const string Suffix = "#]";
        private static readonly Regex _tagRx = new Regex(@"\[#(?<body>.*?)#\]", RegexOptions.Compiled);

        public sealed class Tag
        {
            public string Type;    // "HEAD", "JUGO", "BEB", "TAMAL", "MENU" (si lo usas)
            public string ComboId; // C=...
            public string Cod;     // COD=...
            public string Desc;    // D=...
            public int Q;          // Q=...
            public decimal PU;     // PU=... (precio unitario del combo cabeza)
            public decimal PX;     // PX=... (precio extra subítem)
            public string Notes;   // N=... (notas breves subítem)
        }

        public static string NewComboId()
        {
            var g = Guid.NewGuid().ToString("N").ToUpperInvariant();
            return g.Substring(0, 8); // corto y suficiente
        }

        public static string AppendTag(string userNotes, Tag t)
        {
            var sb = new StringBuilder();
            sb.Append(Prefix);

            // pares clave=valor separados por ';' (sin espacios)
            sb.Append("T=").Append(t.Type ?? "");
            if (!string.IsNullOrEmpty(t.ComboId)) sb.Append(";C=").Append(t.ComboId);
            if (!string.IsNullOrEmpty(t.Cod)) sb.Append(";COD=").Append(t.Cod);
            if (!string.IsNullOrEmpty(t.Desc)) sb.Append(";D=").Append(Sanitize(t.Desc));
            if (t.Q > 0) sb.Append(";Q=").Append(t.Q.ToString(CultureInfo.InvariantCulture));
            if (t.PU > 0) sb.Append(";PU=").Append(t.PU.ToString(CultureInfo.InvariantCulture));
            if (t.PX != 0) sb.Append(";PX=").Append(t.PX.ToString(CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(t.Notes)) sb.Append(";N=").Append(Sanitize(t.Notes));

            sb.Append(Suffix);

            var baseNotes = userNotes ?? "";
            if (baseNotes.Length == 0) return sb.ToString();
            return baseNotes + " " + sb.ToString();
        }

        public static IEnumerable<Tag> ParseAll(string notes)
        {
            if (string.IsNullOrEmpty(notes)) yield break;

            foreach (Match m in _tagRx.Matches(notes))
            {
                var body = m.Groups["body"].Value ?? "";
                var pairs = body.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                var t = new Tag();
                foreach (var p in pairs)
                {
                    var kv = p.Split(new[] { '=' }, 2);
                    if (kv.Length != 2) continue;

                    var k = kv[0].Trim().ToUpperInvariant();
                    var v = kv[1].Trim();

                    if (k == "T") t.Type = v;
                    else if (k == "C") t.ComboId = v;
                    else if (k == "COD") t.Cod = v;
                    else if (k == "D") t.Desc = v;
                    else if (k == "Q") { int q; if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out q)) t.Q = q; }
                    else if (k == "PU") { decimal pu; if (decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out pu)) t.PU = pu; }
                    else if (k == "PX") { decimal px; if (decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out px)) t.PX = px; }
                    else if (k == "N") t.Notes = v;
                }
                if (!string.IsNullOrEmpty(t.Type)) yield return t;
            }
        }

        public static string CleanNotes(string notes)
        {
            if (string.IsNullOrEmpty(notes)) return "";
            var s = _tagRx.Replace(notes, "").Trim();
            return s;
        }

        private static string Sanitize(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            // Evitamos romper el parser; reemplaza separadores raros
            return text.Replace(";", ",").Replace("#", "");
        }
    }
}

