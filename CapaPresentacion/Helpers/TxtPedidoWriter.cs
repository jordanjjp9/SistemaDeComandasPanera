using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CapaEntidad;
using CapaPresentacion.Controles;
using CapaPresentacion.Helpers;

namespace CapaPresentacion
{
    public static class TxtPedidoWriter
    {
        private static readonly bool SIN_TAGS = true;

        public sealed class Resultado
        {
            public string NumPed { get; set; }
            public int CantItems { get; set; }
            public decimal SubTotal { get; set; }
            public decimal Igv { get; set; }
            public decimal Total { get; set; }
            public ceMPedido Cabecera { get; set; }
        }

        private sealed class Plano
        {
            public string Cod10;
            public string Descripcion;
            public int Cantidad;
            public decimal PuConIgv;
            public string Notas;
            public string CdgComb; // "100","101",...
        }

        public delegate (decimal? porIgv, bool? swtIgv)? ResolverTributario(string cod10);

        public static IEnumerable<string> ExtraerCodigosParaFiltrar(IEnumerable<Control> controles)
        {
            if (controles == null) yield break;
            Func<string> nextId = () => "0";
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in FlattenSinTags(controles, nextId))
            {
                var cod = (p.Cod10 ?? "").Trim();
                if (cod.Length > 0 && set.Add(cod))
                    yield return cod;
            }
        }

        public static Resultado Generar(
            IEnumerable<Control> controles,
            Func<string, string> resolverImpresora,
            string cdgVend,
            string cdgUsr,
            string cdgLoc,
            string cdgCaja,
            string numMesa,
            int numPers,
            ResolverTributario resolverTrib = null
        )
        {
            if (controles == null) throw new ArgumentNullException(nameof(controles));

            // ← arranca en 99 para que el primer grupo sea "100"
            int combSeq = 99;
            Func<string> nextGroupId = () => (++combSeq).ToString(CultureInfo.InvariantCulture);

            var planos = FlattenSinTags(controles, nextGroupId).ToList();

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

            foreach (var p in planos)
            {
                var det = new ceDPedido
                {
                    COD10 = p.Cod10,
                    CDG_PROD = ceDPedido.CodigoToInt(p.Cod10),
                    CAN_PPRD = Math.Max(1, p.Cantidad),
                    DESCRIPCION = p.Descripcion ?? "",
                    OBS_PPRD = p.NotesTrimOrEmpty(),
                    CDG_LPRC = 1
                };

                det.SetPreciosDesdePuConIgv(p.PuConIgv, ceMPedido.IGV_TASA);

                if (resolverImpresora != null)
                    det.IMP_PROD = resolverImpresora(p.Cod10) ?? "";

                if (resolverTrib != null)
                {
                    var trib = resolverTrib(p.Cod10);
                    if (trib.HasValue)
                    {
                        if (trib.Value.porIgv.HasValue)
                            SetPropIfExists(det, "POR_IGV", trib.Value.porIgv.Value);
                        if (trib.Value.swtIgv.HasValue)
                            SetPropIfExists(det, "SWT_IGV", trib.Value.swtIgv.Value);
                    }
                }

                // ← asigna el grupo al detalle (la BD lo acolcha)
                if (!string.IsNullOrWhiteSpace(p.CdgComb))
                    det.CDG_COMB = p.CdgComb;

                cab.Detalles.Add(det);
            }

            cab.RecalcularTotales();

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

        public static Resultado Generar(
            Control.ControlCollection lineas,
            Func<string, string> resolverImpresora,
            string cdgVend,
            string cdgUsr,
            string cdgLoc,
            string cdgCaja,
            string numMesa,
            int numPers,
            ResolverTributario resolverTrib = null
        )
        {
            if (lineas == null) throw new ArgumentNullException(nameof(lineas));
            return Generar(
                controles: EnumerarControles(lineas),
                resolverImpresora: resolverImpresora,
                cdgVend: cdgVend,
                cdgUsr: cdgUsr,
                cdgLoc: cdgLoc,
                cdgCaja: cdgCaja,
                numMesa: numMesa,
                numPers: numPers,
                resolverTrib: resolverTrib
            );
        }

        private static void SetPropIfExists(object target, string propName, object value)
        {
            if (target == null || value == null) return;
            var t = target.GetType();
            var p = t.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            if (p == null || !p.CanWrite) return;
            try
            {
                var converted = Convert.ChangeType(value, p.PropertyType, CultureInfo.InvariantCulture);
                p.SetValue(target, converted, null);
            }
            catch { }
        }

        private static IEnumerable<Plano> FlattenSinTags(IEnumerable<Control> controles, Func<string> nextGroupId)
        {
            foreach (var c in controles)
            {
                if (c is ComboPedidoItem combo)
                {
                    foreach (var p in ExportComboSinTags(combo, nextGroupId()))
                        yield return p;
                    continue;
                }

                if (c is MenuPedidoItem menu)
                {
                    foreach (var p in ExportMenuSinTags(menu, nextGroupId()))
                        yield return p;
                    continue;
                }

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
                        Notas = notas,
                        CdgComb = ""
                    };
                }
            }
        }
        // Lee “Notas”, “NotasEncabezado” o “GetNotasEncabezadoRaw” si existen en el control.
        private static string GetNotasAmigable(object obj)
        {
            if (obj == null) return string.Empty;

            // Propiedad Notas
            var p1 = obj.GetType().GetProperty("Notas", BindingFlags.Public | BindingFlags.Instance);
            if (p1 != null)
            {
                try { return Convert.ToString(p1.GetValue(obj, null)) ?? ""; }
                catch { /* ignore */ }
            }

            // Propiedad NotasEncabezado
            var p2 = obj.GetType().GetProperty("NotasEncabezado", BindingFlags.Public | BindingFlags.Instance);
            if (p2 != null)
            {
                try { return Convert.ToString(p2.GetValue(obj, null)) ?? ""; }
                catch { /* ignore */ }
            }

            // Método GetNotasEncabezadoRaw()
            var m = obj.GetType().GetMethod("GetNotasEncabezadoRaw", BindingFlags.Public | BindingFlags.Instance);
            if (m != null)
            {
                try { return Convert.ToString(m.Invoke(obj, null)) ?? ""; }
                catch { /* ignore */ }
            }

            return string.Empty;
        }

        private static IEnumerable<Plano> ExportComboSinTags(ComboPedidoItem combo, string gid)
        {
            var tipo = combo.GetType();

            string codHead = Get<string>(combo, "Codigo", "");
            string descHead = Get<string>(combo, "Descripcion", "");
            int qHead = Get<int>(combo, "Cantidad", 1);
            decimal puHead = GetHeadPuConIgv(tipo, combo);
            string notasHead = GetNotasEncabezadoAmigable(combo);

            yield return new Plano
            {
                Cod10 = codHead,
                Descripcion = descHead,
                Cantidad = qHead,
                PuConIgv = puHead,
                Notas = (notasHead ?? string.Empty).Trim(),
                CdgComb = gid
            };

            foreach (var j in ExportSubitems(tipo, combo, "ExportJugos"))
            {
                string cod = TryGet<string>(j, "Codigo", "");
                string des = TryGet<string>(j, "Descripcion", "");
                int can = Math.Max(1, TryGet<int>(j, "Cantidad", 1));
                string notas = TryGet<string>(j, "Notas", "");

                yield return new Plano
                {
                    Cod10 = cod,
                    Descripcion = des,
                    Cantidad = can,
                    PuConIgv = 0m,
                    Notas = (notas ?? string.Empty).Trim(),
                    CdgComb = gid
                };
            }

            foreach (var b in ExportSubitems(tipo, combo, "ExportBebidas"))
            {
                string cod = TryGet<string>(b, "Codigo", "");
                string des = TryGet<string>(b, "Descripcion", "");
                int can = Math.Max(1, TryGet<int>(b, "Cantidad", 1));
                string notas = TryGet<string>(b, "Notas", "");

                yield return new Plano
                {
                    Cod10 = cod,
                    Descripcion = des,
                    Cantidad = can,
                    PuConIgv = 0m,
                    Notas = (notas ?? string.Empty).Trim(),
                    CdgComb = gid
                };
            }

            foreach (var t in ExportSubitems(tipo, combo, "ExportTamales"))
            {
                string cod = TryGet<string>(t, "Codigo", "");
                string des = TryGet<string>(t, "Descripcion", "");
                int can = Math.Max(1, TryGet<int>(t, "Cantidad", 1));

                yield return new Plano
                {
                    Cod10 = cod,
                    Descripcion = des,
                    Cantidad = can,
                    PuConIgv = 0m,
                    Notas = string.Empty,
                    CdgComb = gid
                };
            }

            var m = GetMethod(tipo, "GetLineasExport");
            if (m != null)
            {
                var list = m.Invoke(combo, null) as System.Collections.IEnumerable;
                if (list != null)
                {
                    bool first = true;
                    foreach (var ln in list)
                    {
                        string cod = TryGet<string>(ln, "Codigo", "");
                        string des = TryGet<string>(ln, "Descripcion", "");
                        int can = Math.Max(1, TryGet<int>(ln, "Cantidad", 1));
                        decimal pu = TryGet<decimal>(ln, "PrecioUnitarioConIgv", 0m);
                        string notas = TryGet<string>(ln, "Notas", "");

                        if (first) { first = false; continue; }

                        yield return new Plano
                        {
                            Cod10 = cod,
                            Descripcion = des,
                            Cantidad = can,
                            PuConIgv = 0m,
                            Notas = (notas ?? string.Empty).Trim(),
                            CdgComb = gid
                        };
                    }
                }
            }
        }

        private static IEnumerable<Plano> ExportMenuSinTags(MenuPedidoItem menu, string gid)
        {
            var tipo = menu.GetType();

            var m = GetMethod(tipo, "GetLineasExport");
            if (m != null)
            {
                var list = m.Invoke(menu, null) as System.Collections.IEnumerable;
                if (list != null)
                {
                    bool first = true;
                    foreach (var ln in list)
                    {
                        string cod = TryGet<string>(ln, "Codigo", "");
                        string des = TryGet<string>(ln, "Descripcion", "");
                        int can = Math.Max(1, TryGet<int>(ln, "Cantidad", 1));
                        decimal pu = TryGet<decimal>(ln, "PrecioUnitarioConIgv", 0m);
                        string notas = TryGet<string>(ln, "Notas", "");

                        if (first)
                        {
                            first = false;
                            yield return new Plano
                            {
                                Cod10 = cod,
                                Descripcion = des,
                                Cantidad = can,
                                PuConIgv = pu,
                                Notas = (notas ?? string.Empty).Trim(),
                                CdgComb = gid
                            };
                        }
                        else
                        {
                            yield return new Plano
                            {
                                Cod10 = cod,
                                Descripcion = des,
                                Cantidad = can,
                                PuConIgv = 0m,
                                Notas = (notas ?? string.Empty).Trim(),
                                CdgComb = gid
                            };
                        }
                    }
                    yield break;
                }
            }

            string codHead = Get<string>(menu, "Codigo", "");
            string descHead = Get<string>(menu, "Descripcion", "");
            int qHead = Get<int>(menu, "Cantidad", 1);
            decimal puHead = GetHeadPuConIgv(tipo, menu);
            string notasHead = GetNotasEncabezadoAmigable(menu);

            yield return new Plano
            {
                Cod10 = codHead,
                Descripcion = descHead,
                Cantidad = qHead,
                PuConIgv = puHead,
                Notas = (notasHead ?? string.Empty).Trim(),
                CdgComb = gid
            };
        }

        private static System.Collections.IEnumerable ExportSubitems(Type t, object instance, string methodName)
        {
            var m = GetMethod(t, methodName);
            if (m == null) return Empty();
            var obj = m.Invoke(instance, null) as System.Collections.IEnumerable;
            return obj ?? Empty();
        }

        private static IEnumerable<object> Empty() { yield break; }

        private static decimal GetHeadPuConIgv(Type t, object instance)
        {
            var p1 = t.GetProperty("PrecioUnitarioConIgv", BindingFlags.Public | BindingFlags.Instance);
            if (p1 != null) { try { return Convert.ToDecimal(p1.GetValue(instance, null), CultureInfo.InvariantCulture); } catch { } }
            var p2 = t.GetProperty("PrecioUnitario", BindingFlags.Public | BindingFlags.Instance);
            if (p2 != null) { try { return Convert.ToDecimal(p2.GetValue(instance, null), CultureInfo.InvariantCulture); } catch { } }
            return 0m;
        }

        private static string GetNotasEncabezadoAmigable(object obj)
        {
            if (obj == null) return string.Empty;
            var p2 = obj.GetType().GetProperty("NotasEncabezado", BindingFlags.Public | BindingFlags.Instance);
            if (p2 != null) try { return Convert.ToString(p2.GetValue(obj, null)) ?? ""; } catch { }
            var m = obj.GetType().GetMethod("GetNotasEncabezadoRaw", BindingFlags.Public | BindingFlags.Instance);
            if (m != null) try { return Convert.ToString(m.Invoke(obj, null)) ?? ""; } catch { }
            var p1 = obj.GetType().GetProperty("Notas", BindingFlags.Public | BindingFlags.Instance);
            if (p1 != null) try { return Convert.ToString(p1.GetValue(obj, null)) ?? ""; } catch { }
            return string.Empty;
        }

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
            => obj?.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance) != null;

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

        private static MethodInfo GetMethod(Type t, string name)
            => t?.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);

        private static string NotesTrimOrEmpty(this Plano p)
            => (p.Notas ?? "").Trim();
    }
}
