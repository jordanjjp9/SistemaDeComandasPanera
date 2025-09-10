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
        // === Carpeta destino solicitada ===
        private static readonly string CARPETA_DESTINO =
            @"C:\Users\lenovo\Documents\DocumentosPrueba";

        public sealed class Resultado
        {
            public string NumPed { get; set; }
            public string RutaH { get; set; }
            public string RutaD { get; set; }
            public int CantItems { get; set; }
            public decimal SubTotal { get; set; }
            public decimal Igv { get; set; }
            public decimal Total { get; set; }
        }

        // Línea plana a partir de los controles de la UI
        private sealed class Plano
        {
            public string Cod10;         // CDG_PROD (10 dígitos si es posible)
            public string Descripcion;
            public int Cantidad;
            public decimal PuConIgv;     // PU mostrado en UI (CON IGV)
            public string Notas;         // OBS_PPRD
        }

        // Resolver opcional de datos tributarios por producto (M_PROD / M_PRODUC)
        // Devuelve (POR_IGV, SWT_IGV). Si no puedes resolverlos, retorna nulls.
        public delegate (decimal? porIgv, bool? swtIgv)? ResolverTributario(string cod10);

        public static Resultado GenerarTxts(
            Control.ControlCollection lineas,
            Func<string, string> resolverImpresora, // IMP_PROD
            string cdgVend,
            string cdgUsr,
            string cdgLoc,
            string cdgCaja,
            string numMesa,
            int numPers,
            ResolverTributario resolverTrib = null // opcional
        )
        {
            if (lineas == null) throw new ArgumentNullException(nameof(lineas));

            // === 1) Aplanar la UI a una lista de “plano” (solo top-level) ===
            var planos = Flatten(lineas).ToList();

            // === 2) Armar cabecera/entidad para totales y M_PEDIDO ===
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

            // Convertir “plano” a detalles para que ceMPedido calcule totales
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
                det.SetPreciosDesdePuConIgv(p.PuConIgv, ceMPedido.IGV_TASA); // calcula PRE_PPRD/IMP_TPRD/PRE_IGV/IMP_IGV
                cab.Detalles.Add(det);
            }
            cab.RecalcularTotales();

            // === 3) Exportar con DAO (M_PEDIDO + D básico) ===
            Directory.CreateDirectory(CARPETA_DESTINO);
            var svc = new cnPedido();
            var (mPath, dPath) = svc.ExportarTxt(cab, CARPETA_DESTINO, incluirEncabezados: true);

            // === 4) Reescribir D_PEDIDO con layout extendido (mapea TODO) ===
            var header = string.Join("|", new[]
            {
                "NUM_PED","CDG_PROD","CDG_FPRD","CAN_PPRD","PRE_PPRD","DCT_PPRD","DCT_FIC","IGV_PPRD","IMP_TPRD",
                "CAN_DPRD","CAN_FPRD","OBS_PPRD","CDG_LPRC","PRE_IGV","IMP_IGV",
                "FAC_UVTA","CDG_UVTA","COM_PPRD","CAN_PROD","CAN_OTRB","CAN_UVTA","PRE_UVTA","VAL_UVTA","TOT_UVTA",
                "POR_TISC","SWT_IGV","COM_IMPO","POR_IGV","IMP_IVA","NUM_ITEM","IMP_PROD","SWT_IMPR","PCT_CARG","IMP_CARG"
            });

            using (var wd = new StreamWriter(dPath, false, Encoding.UTF8))
            {
                wd.WriteLine(header);

                int itemN = 0;
                for (int i = 0; i < planos.Count; i++)
                {
                    var p = planos[i];
                    var det = cab.Detalles[i];
                    itemN++;

                    string cdgProd10 = To10Digits(p.Cod10);
                    bool precioCero = (p.PuConIgv <= 0m + 0.0000001m);

                    // Resolver IMP_PROD (impresora) y banderas
                    string impProd = resolverImpresora?.Invoke(p.Cod10) ?? "";
                    string swtImpr = string.IsNullOrWhiteSpace(impProd) ? "" : "X";

                    // Resolver tributación específica por producto (opcional)
                    decimal porIgvProd = cab.POR_IGV;
                    string swtIgv = "";
                    if (resolverTrib != null)
                    {
                        var r = resolverTrib(p.Cod10);
                        if (r.HasValue)
                        {
                            if (r.Value.porIgv.HasValue) porIgvProd = r.Value.porIgv.Value;
                            if (r.Value.swtIgv.HasValue && r.Value.swtIgv.Value) swtIgv = "X";
                        }
                    }

                    // Campos “core”
                    string CAN_PPRD = det.CAN_PPRD.ToString("0.0000", CultureInfo.InvariantCulture);
                    string PRE_PPRD = (precioCero ? 0m : det.PRE_PPRD).ToString("0.0000", CultureInfo.InvariantCulture);
                    string DCT_PPRD = "0.00";
                    string DCT_FIC = "0.00";
                    string IGV_PPRD = "0.00";
                    string IMP_TPRD = (precioCero ? 0m : det.IMP_TPRD).ToString("0.00", CultureInfo.InvariantCulture);

                    string CAN_DPRD = ""; // vacío
                    string CAN_FPRD = ""; // vacío

                    string OBS_PPRD = det.OBS_PPRD ?? "";
                    string CDG_LPRC = "001";

                    string PRE_IGV = (precioCero ? 0m : det.PRE_IGV).ToString("0.0000", CultureInfo.InvariantCulture);
                    string IMP_IGV = (precioCero ? 0m : det.IMP_IGV).ToString("0.00", CultureInfo.InvariantCulture);

                    // Campos “extendidos” → estáticos cuando precio = 0
                    string FAC_UVTA = precioCero ? "1.0000000000" : "";
                    string CDG_UVTA = precioCero ? "001" : "";
                    string COM_PPRD = precioCero ? "0.00" : "";
                    string CAN_PROD = precioCero ? "0.0000" : "";
                    string CAN_OTRB = precioCero ? "0.0000" : "";
                    string CAN_UVTA = precioCero ? "1.0000" : "";
                    string PRE_UVTA = precioCero ? "0.0000" : "";
                    string VAL_UVTA = precioCero ? "0.0000" : "";
                    string TOT_UVTA = precioCero ? "0.00" : "";
                    string POR_TISC = precioCero ? "0.00" : "";
                    string SWT_IGV = precioCero ? swtIgv : "";
                    string COM_IMPO = precioCero ? "0.00" : "";
                    string POR_IGV = precioCero ? porIgvProd.ToString("0.00", CultureInfo.InvariantCulture) : "";
                    string IMP_IVA = precioCero ? "0.00" : "";
                    string NUM_ITEM = itemN.ToString(CultureInfo.InvariantCulture);
                    string IMP_PROD = impProd;
                    string SWT_IMPR = swtImpr;
                    string PCT_CARG = precioCero ? "0.00" : "";
                    string IMP_CARG = precioCero ? "0.00" : "";

                    var cols = new[]
                    {
                        cab.NUM_PED ?? "",
                        cdgProd10,
                        "",                         // CDG_FPRD
                        CAN_PPRD, PRE_PPRD, DCT_PPRD, DCT_FIC, IGV_PPRD, IMP_TPRD,
                        CAN_DPRD, CAN_FPRD, OBS_PPRD, CDG_LPRC, PRE_IGV, IMP_IGV,
                        FAC_UVTA, CDG_UVTA, COM_PPRD, CAN_PROD, CAN_OTRB, CAN_UVTA, PRE_UVTA, VAL_UVTA, TOT_UVTA,
                        POR_TISC, SWT_IGV, COM_IMPO, POR_IGV, IMP_IVA, NUM_ITEM, IMP_PROD, SWT_IMPR, PCT_CARG, IMP_CARG
                    };
                    wd.WriteLine(string.Join("|", cols));
                }
            }

            return new Resultado
            {
                NumPed = cab.NUM_PED,
                RutaH = mPath,
                RutaD = dPath,
                CantItems = cab.Detalles.Count,
                SubTotal = cab.IMP_BASE,
                Igv = cab.IMP_IGV,
                Total = cab.IMP_TOT
            };
        }

        // ——————————————————————————————
        // Helpers
        // ——————————————————————————————

        // Aplana SOLO controles top-level; nada de recursión para evitar duplicados.
        private static IEnumerable<Plano> Flatten(Control.ControlCollection lineas)
        {
            foreach (Control c in lineas)
            {
                // === COMBO: el propio control ya devuelve encabezado + sublíneas ===
                if (c is ComboPedidoItem ci)
                {
                    foreach (var ln in ci.GetLineasExport())
                    {
                        string cod = Get<string>(ln, "Codigo", "");
                        string desc = Get<string>(ln, "Descripcion", "");
                        int qty = Get<int>(ln, "Cantidad", 1);
                        decimal pu = Get<decimal>(ln, "PrecioUnitarioConIgv", 0m);
                        string notas = Get<string>(ln, "Notas", "");

                        yield return new Plano
                        {
                            Cod10 = (cod ?? "").Trim(),
                            Descripcion = desc ?? "",
                            Cantidad = qty,
                            PuConIgv = pu,         // encabezado >0 ; sublíneas == 0 (por el cambio anterior)
                            Notas = notas ?? ""
                        };
                    }
                    continue;
                }

                // === MENÚ: ya devuelves ambas líneas (menú + chicha) ===
                if (c is MenuPedidoItem mi)
                {
                    foreach (var ln in mi.GetLineasExport())
                    {
                        yield return new Plano
                        {
                            Cod10 = (ln.Codigo ?? "").Trim(),
                            Descripcion = ln.Descripcion ?? "",
                            Cantidad = ln.Cantidad,
                            PuConIgv = ln.PrecioUnitarioConIgv,
                            Notas = ln.Notas ?? ""
                        };
                    }
                    continue;
                }

                // === Línea “normal” ===
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

        // Lee Notas / NotasEncabezado / GetNotasEncabezadoRaw si existen
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

        private static string To10Digits(string codigo)
        {
            string s = (codigo ?? "").Trim();
            if (s.Length == 0) return "0000000000";
            if (s.All(char.IsDigit)) return s.PadLeft(10, '0');
            var digits = new string(s.Where(char.IsDigit).ToArray());
            return digits.Length > 0 ? digits.PadLeft(10, '0') : "0000000000";
        }
    }
}
