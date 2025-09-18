using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using CapaEntidad;

namespace CapaNegocio.MetaStore
{
    public class CsvMetaStore : IMetaStore
    {
        private readonly string _root;

        public CsvMetaStore(string root)
        {
            if (string.IsNullOrEmpty(root)) throw new ArgumentNullException("root");
            _root = root;
        }

        private string GetFilePath(string numPed, DateTime? when)
        {
            string yyyymm = (when ?? DateTime.Now).ToString("yyyyMM");
            string dir = Path.Combine(_root, yyyymm);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return Path.Combine(dir, numPed + ".txt");
        }

        private static string Escape(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\r", " ").Replace("\n", " ").Replace("|", "/");
        }

        private static string[] SplitSafe(string line)
        {
            return (line ?? "").Split(new[] { '|' }, StringSplitOptions.None);
        }

        public void Save(string numPed, int secItem, List<DetalleMeta> meta, string hash)
        {
            string path = GetFilePath(numPed, null);

            var lines = new List<string>();
            if (File.Exists(path))
            {
                lines.AddRange(File.ReadAllLines(path, Encoding.UTF8));
                lines.RemoveAll(l =>
                {
                    var parts = SplitSafe(l);
                    if (parts.Length < 8) return false;
                    int secTmp; if (!int.TryParse(parts[0], out secTmp)) return false;
                    return secTmp == secItem;
                });
            }

            if (meta != null)
            {
                for (int i = 0; i < meta.Count; i++)
                {
                    var m = meta[i];
                    string line = string.Join("|", new[]
                    {
                        secItem.ToString(),
                        m.Ord.ToString(),
                        Escape(m.Tipo),
                        Escape(m.Cod),
                        Escape(m.Desc),
                        Escape(m.CategoriaId),
                        Escape(m.Flags),
                        Escape(hash)
                    });
                    lines.Add(line);
                }
            }

            string tmp = path + ".tmp";
            File.WriteAllLines(tmp, lines.ToArray(), Encoding.UTF8);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public List<DetalleMeta> Load(string numPed, int secItem, string hash)
        {
            if (!Directory.Exists(_root)) return new List<DetalleMeta>();
            var metas = new List<DetalleMeta>();

            foreach (var sub in Directory.GetDirectories(_root))
            {
                string path = Path.Combine(sub, numPed + ".txt");
                if (!File.Exists(path)) continue;

                var lines = File.ReadAllLines(path, Encoding.UTF8);
                foreach (var l in lines)
                {
                    var p = SplitSafe(l);
                    if (p.Length < 8) continue;

                    int secTmp, ordTmp;
                    if (!int.TryParse(p[0], out secTmp)) continue;
                    if (secTmp != secItem) continue;
                    if (!int.TryParse(p[1], out ordTmp)) ordTmp = 0;

                    if (!string.IsNullOrEmpty(hash) && !string.Equals(p[7], hash, StringComparison.Ordinal))
                        return new List<DetalleMeta>();

                    metas.Add(new DetalleMeta
                    {
                        Ord = ordTmp,
                        Tipo = p[2],
                        Cod = p[3],
                        Desc = p[4],
                        CategoriaId = p[5],
                        Flags = p[6]
                    });
                }
                break;
            }
            return metas;
        }

        // NUEVO: borrar el archivo del pedido (buscando en todos los meses)
        public void DeletePedido(string numPed)
        {
            if (!Directory.Exists(_root)) return;

            foreach (var sub in Directory.GetDirectories(_root))
            {
                var path = Path.Combine(sub, numPed + ".txt");
                if (!File.Exists(path)) continue;

                // retry liviano por si hay lock momentáneo
                for (int i = 0; i < 3; i++)
                {
                    try { File.Delete(path); break; }
                    catch { System.Threading.Thread.Sleep(120); }
                }

                // Si quedó la carpeta del mes vacía, la quitamos (opcional)
                try
                {
                    if (Directory.Exists(sub) && Directory.GetFiles(sub).Length == 0)
                        Directory.Delete(sub);
                }
                catch { /* ignora */ }
            }
        }

        // (Sigue disponible) ComputeHash para validar cambios del detalle
        public static string ComputeHash(params string[] parts)
        {
            var sb = new StringBuilder();
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i > 0) sb.Append("|");
                    sb.Append((parts[i] ?? string.Empty).ToUpperInvariant());
                }
            }
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var s = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) s.Append(hash[i].ToString("X2"));
                return s.ToString();
            }
        }
    }
}
