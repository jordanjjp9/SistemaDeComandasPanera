using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Controles;
using CapaPresentacion.Helpers;

namespace CapaPresentacion
{
    public static class TxtPedidoWriter
    {
        public sealed class Resultado
        {
            public string NumPed { get; set; }
            public int CantItems { get; set; }
            public decimal SubTotal { get; set; }
            public decimal Igv { get; set; }
            public decimal Total { get; set; }

            /// <summary>Cabecera lista para guardar en la BD (incluye Detalles).</summary>
            public ceMPedido Cabecera { get; set; }
        }

        // Línea plana intermedia
        private sealed class Plano
        {
            public string Cod10;             // CDG_PROD (10 dígitos si aplica)
            public string Descripcion;
            public int Cantidad;
            public decimal PuConIgv;         // Precio unitario mostrado en UI (CON IGV)
            public string Notas;             // OBS_PPRD
        }

        /// <summary>
        /// Resolver opcional (si quieres obtener POR_IGV/SWT_IGV del maestro por producto).
        /// Devuelve (porIgv, swtIgv). Si no puedes resolver, retorna null.
        /// </summary>
        public delegate (decimal? porIgv, bool? swtIgv)? ResolverTributario(string cod10);

        /// <summary>
        /// Mapea la UI a ceMPedido (+ ceDPedido). NO exporta TXT. Devuelve resumen y la cabecera para BD.
        /// </summary>
        public static Resultado Generar(
            Control.ControlCollection lineas,
            Func<string, string> resolverImpresora,   // IMP_PROD
            string cdgVend,
            string cdgUsr,
            string cdgLoc,
            string cdgCaja,
            string numMesa,
            int numPers,
            ResolverTributario resolverTrib = null    // opcional
        )
        {
            if (lineas == null) throw new ArgumentNullException(nameof(lineas));

            // 1) Aplanar controles a líneas planas (respeta ComboPedidoItem y MenuPedidoItem)
            var planos = Flatten(lineas).ToList();

            // 2) Cabecera
            string numPed = DateTime.Now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture);
            var cab = new ceMPedido
            {
                NUM_PED = numPed,
                FEC_PED = DateTime.Now,
                CDG_VEND = (cdgVend ?? "").Trim(),
                CDG_MON = "S",
                CDG_LOC = (cdgLoc ?? "001").Trim(),
                CDG_AMB = (SesionActual.Ambiente ?? "").Trim(),
                NUM_MESA = (numMesa ?? "").Trim(),
                CDG_USR = (cdgUsr ?? "").Trim(),
                POR_IGV = ceMPedido.IGV_TASA
            };
            if (numPers > 0) cab.NUM_PERS = numPers;

            // 3) Detalles
            foreach (var p in planos)
            {
                var det = new ceDPedido
                {
                    COD10 = p.Cod10,
                    CDG_PROD = ceDPedido.CodigoToInt(p.Cod10),
                    CAN_PPRD = Math.Max(1, p.Cantidad),
                    DESCRIPCION = p.Descripcion ?? "",
                    OBS_PPRD = p.Notas?.Trim() ?? "",
                    CDG_LPRC = 1
                };

                // Precio SIN/CON IGV desde PU mostrado en UI (CON IGV)
                det.SetPreciosDesdePuConIgv(p.PuConIgv, ceMPedido.IGV_TASA);

                // IMP_PROD (impresora) si te interesa conservarlo en entidad
                if (resolverImpresora != null)
                    det.IMP_PROD = resolverImpresora(p.Cod10) ?? "";

                cab.Detalles.Add(det);
            }

            cab.RecalcularTotales();

            // 4) Resultado
            return new Resultado
            {
                NumPed = cab.NUM_PED,
                CantItems = cab.Detalles.Count,
                SubTotal = cab.IMP_BASE,
                Igv = cab.IMP_IGV,
                Total = cab.IMP_TOT,
                Cabecera = cab
            };
        }

        // ——————————————————————
        // Helpers
        // ——————————————————————

        /// <summary>
        /// Devuelve una secuencia de líneas “planas” a partir de los controles (incluye sublíneas).
        /// </summary>
        private static IEnumerable<Plano> Flatten(Control.ControlCollection lineas)
        {
            foreach (Control c in EnumerarControles(lineas))
            {
                // 1) ComboPedidoItem -> usa su GetLineasExport (ya incluye encabezado + subítems)
                if (c is ComboPedidoItem ci)
                {
                    foreach (var ln in ci.GetLineasExport())
                    {
                        // ln es ComboPedidoItem.LineaExport
                        string cod = TryGet<string>(ln, "Codigo", "");
                        string desc = TryGet<string>(ln, "Descripcion", "");
                        int can = TryGet<int>(ln, "Cantidad", 1);
                        decimal pu = TryGet<decimal>(ln, "PrecioUnitarioConIgv", 0m);
                        string notas = TryGet<string>(ln, "Notas", "");

                        yield return new Plano
                        {
                            Cod10 = cod,
                            Descripcion = desc,
                            Cantidad = can,
                            PuConIgv = pu,
                            Notas = notas
                        };
                    }
                    continue;
                }

                // 2) MenuPedidoItem -> usa su GetLineasExport (menú + chicha)
                if (c is MenuPedidoItem mi)
                {
                    foreach (var ln in mi.GetLineasExport())
                    {
                        // ln es MenuPedidoItem.LineaExport
                        string cod = TryGet<string>(ln, "Codigo", "");
                        string desc = TryGet<string>(ln, "Descripcion", "");
                        int can = TryGet<int>(ln, "Cantidad", 1);
                        decimal pu = TryGet<decimal>(ln, "PrecioUnitarioConIgv", 0m);
                        string notas = TryGet<string>(ln, "Notas", "");

                        yield return new Plano
                        {
                            Cod10 = cod,
                            Descripcion = desc,
                            Cantidad = can,
                            PuConIgv = pu,
                            Notas = notas
                        };
                    }
                    continue;
                }

                // 3) Línea normal (Lineas sueltas)
                if (HasProp(c, "Codigo"))
                {
                    string cod = Get<string>(c, "Codigo", "") ?? "";
                    int can = Get<int>(c, "Cantidad", 1);
                    decimal pu = Get<decimal>(c, "PrecioUnitario", 0m);
                    string desc = Get<string>(c, "Descripcion", "") ?? "";
                    string notas = GetNotasAmigable(c) ?? "";

                    yield return new Plano
                    {
                        Cod10 = cod,
                        Descripcion = desc,
                        Cantidad = can,
                        PuConIgv = pu,
                        Notas = notas
                    };
                }
            }
        }

        /// <summary>Enumera recursivamente todos los controles.</summary>
        private static IEnumerable<Control> EnumerarControles(Control.ControlCollection col)
        {
            foreach (Control c in col)
            {
                yield return c;
                foreach (Control h in EnumerarControles(c.Controls))
                    yield return h;
            }
        }

        private static bool HasProp(object obj, string prop)
        {
            return obj?.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance) != null;
        }

        private static T Get<T>(object obj, string prop, T def)
        {
            if (obj == null) return def;
            var p = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            if (p == null) return def;
            try
            {
                var v = p.GetValue(obj, null);
                if (v == null) return def;
                return (T)Convert.ChangeType(v, typeof(T), CultureInfo.InvariantCulture);
            }
            catch { return def; }
        }

        private static T TryGet<T>(object obj, string prop, T def)
        {
            if (obj == null) return def;
            var p = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            if (p == null) return def;
            try
            {
                var v = p.GetValue(obj, null);
                if (v == null) return def;
                return (T)Convert.ChangeType(v, typeof(T), CultureInfo.InvariantCulture);
            }
            catch { return def; }
        }

        /// <summary>Lee “Notas”, “NotasEncabezado” o “GetNotasEncabezadoRaw” si existen.</summary>
        private static string GetNotasAmigable(object obj)
        {
            if (obj == null) return string.Empty;

            var p1 = obj.GetType().GetProperty("Notas", BindingFlags.Public | BindingFlags.Instance);
            if (p1 != null) try { return Convert.ToString(p1.GetValue(obj, null)) ?? ""; } catch { }

            var p2 = obj.GetType().GetProperty("NotasEncabezado", BindingFlags.Public | BindingFlags.Instance);
            if (p2 != null) try { return Convert.ToString(p2.GetValue(obj, null)) ?? ""; } catch { }

            var m = obj.GetType().GetMethod("GetNotasEncabezadoRaw", BindingFlags.Public | BindingFlags.Instance);
            if (m != null) try { return Convert.ToString(m.Invoke(obj, null)) ?? ""; } catch { }

            return string.Empty;
        }
    }
}
