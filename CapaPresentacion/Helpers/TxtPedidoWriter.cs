using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Helpers;

namespace CapaPresentacion.Helpers
{
    public static class TxtPedidoWriter
    {
        // === Contrato que usa frmMenuPrincipal para el MessageBox ===
        public sealed class Resultado
        {
            public string NumPed { get; set; }
            public string RutaH { get; set; }
            public string RutaD { get; set; }
            public int CantItems { get; set; }
            public decimal SubTotal { get; set; } // Base imponible (sin IGV)
            public decimal Igv { get; set; }
            public decimal Total { get; set; }
        }

        private const string CARPETA_DESTINO = @"C:\Users\lenovo\Documents\DocumentosPrueba";
        public static Resultado GenerarTxts(
            Control.ControlCollection lineas,
            Func<string, string> resolverImpresora,
            string cdgVend,
            string cdgUsr,
            string cdgLoc,
            string cdgCaja,
            string numMesa,
            int numPers)
        {
            if (lineas == null) throw new ArgumentNullException(nameof(lineas));

            // === 1) Cabecera ===
            string numPed = DateTime.Now.ToString("yyMMddHHmmss", CultureInfo.InvariantCulture);

            var cab = new ceMPedido
            {
                NUM_PED = numPed,
                FEC_PED = DateTime.Now,
                CDG_VEND = (cdgVend ?? "").Trim(),
                CDG_MON = "S",
                CDG_LOC = (cdgLoc ?? "001").Trim(),
                CDG_AMB = (SesionActual.Ambiente ?? "").Trim(), // SesionActual es estático
                NUM_MESA = (numMesa ?? "").Trim(),
                CDG_USR = (cdgUsr ?? "").Trim(),
                POR_IGV = ceMPedido.IGV_TASA
            };
            if (numPers > 0) cab.NUM_PERS = numPers;

            // === 2) Detalle desde los controles (Líneas / Combos / Menús) ===
            foreach (Control ctrl in EnumerarControles(lineas))
            {
                // Requiere al menos la propiedad "Codigo" para considerar el control una línea válida
                if (!HasProp(ctrl, "Codigo")) continue;

                string cod10 = Get<string>(ctrl, "Codigo", "");
                if (string.IsNullOrWhiteSpace(cod10)) continue;

                int cantidad = Get<int>(ctrl, "Cantidad", 1);
                decimal puConIgv = Get<decimal>(ctrl, "PrecioUnitario", 0m);  // PU mostrado en UI (CON IGV)
                string desc = Get<string>(ctrl, "Descripcion", "") ?? "";
                string notas = GetNotasAmigable(ctrl) ?? "";               // 👈 toma Notas / NotasEncabezado / métodos Raw

                var det = new ceDPedido
                {
                    COD10 = cod10,
                    CDG_PROD = ceDPedido.CodigoToInt(cod10),
                    CAN_PPRD = Math.Max(1, cantidad),
                    DESCRIPCION = desc,
                    OBS_PPRD = notas.Trim(),              // BLANCO si vacío (DAO lo respeta)
                    CDG_LPRC = 1,                         // Lista 001 (el DAO lo emite como "001")
                    IMP_PROD = (resolverImpresora != null) ? (resolverImpresora(cod10) ?? "") : ""
                };

                det.SetPreciosDesdePuConIgv(puConIgv, ceMPedido.IGV_TASA);
                cab.Detalles.Add(det);
            }

            cab.RecalcularTotales();

            // === 3) Exportar TXT (con ENCABEZADOS) ===
            var svc = new cnPedido();
            Directory.CreateDirectory(CARPETA_DESTINO);
            var (mPath, dPath) = svc.ExportarTxt(cab, CARPETA_DESTINO, incluirEncabezados: true);

            // === 4) Resultado para el form ===
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


        // ===================== Helpers internos =====================

        private static System.Collections.Generic.IEnumerable<Control> EnumerarControles(Control.ControlCollection root)
        {
            if (root == null) yield break;
            var stack = new System.Collections.Generic.Stack<Control>();
            foreach (Control c in root) stack.Push(c);

            while (stack.Count > 0)
            {
                var cur = stack.Pop();
                yield return cur;
                foreach (Control child in cur.Controls)
                    stack.Push(child);
            }
        }

        private static bool HasProp(object obj, string prop)
        {
            if (obj == null) return false;
            return obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance) != null;
        }

        private static T Get<T>(object obj, string prop, T def = default(T))
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

        /// <summary>
        /// Devuelve las notas desde la propiedad 'Notas', 'NotasEncabezado' o métodos
        /// 'GetNotasRaw' / 'GetNotasEncabezadoRaw' si existen.
        /// </summary>
        private static string GetNotasAmigable(object ctrl)
        {
            // Acumulador
            string acum = null;

            // Helper para concatenar con salto de línea
            string Append(string cur, string add)
            {
                if (string.IsNullOrWhiteSpace(add)) return cur;
                add = add.Trim();
                return string.IsNullOrEmpty(cur) ? add : (cur + Environment.NewLine + add);
            }

            // 1) Propiedades directas
            var n1 = Get<string>(ctrl, "Notas", "");
            if (!string.IsNullOrWhiteSpace(n1)) acum = Append(acum, n1);

            var n2 = Get<string>(ctrl, "NotasEncabezado", "");
            if (!string.IsNullOrWhiteSpace(n2)) acum = Append(acum, n2);

            // 2) Métodos sin parámetros
            try
            {
                var m0 = ctrl.GetType().GetMethod("GetNotasRaw", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (m0 != null)
                {
                    var s = m0.Invoke(ctrl, null) as string;
                    if (!string.IsNullOrWhiteSpace(s)) acum = Append(acum, s);
                }

                var m1 = ctrl.GetType().GetMethod("GetNotasEncabezadoRaw", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                if (m1 != null)
                {
                    var s = m1.Invoke(ctrl, null) as string;
                    if (!string.IsNullOrWhiteSpace(s)) acum = Append(acum, s);
                }
            }
            catch { /* ignore */ }

            // 3) Método con 1 parámetro (enum), típico en MenuPedidoItem: GetNotasRaw(zona)
            try
            {
                var metodos = ctrl.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                foreach (var m in metodos)
                {
                    if (!string.Equals(m.Name, "GetNotasRaw", StringComparison.Ordinal)) continue;
                    var pars = m.GetParameters();
                    if (pars.Length != 1) continue;

                    var pType = pars[0].ParameterType;
                    if (!pType.IsEnum) continue;

                    foreach (var val in Enum.GetValues(pType))
                    {
                        try
                        {
                            var s = m.Invoke(ctrl, new object[] { val }) as string;
                            if (!string.IsNullOrWhiteSpace(s)) acum = Append(acum, s);
                        }
                        catch { /* intentamos todas las zonas */ }
                    }
                }
            }
            catch { /* ignore */ }

            return acum ?? string.Empty;
        }

    }
}
