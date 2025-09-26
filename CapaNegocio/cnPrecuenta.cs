using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaDatos;

namespace CapaNegocio
{
    public class cnPrecuenta
    {
        private readonly DAOPrecuenta _dao = new DAOPrecuenta();

        /// <summary>
        /// Genera el dataset final para el RDLC de Precuenta partiendo del SP dbo.Pedido (sin modificarlo).
        /// - usarPrecioConIgv: true => usa PRE_IGV y calcula SubTotal = PRE_IGV * Cantidad;
        ///                      false => usa PRE_PPRD e IMP_TPRD de la BD.
        /// - incluirIGV0: si true, hace un merge con las líneas que tengan IGV=0 (y que Pedido no devuelve).
        /// Retorna un DataTable listo para enlazar a ReportViewer (dsPrecuenta).
        /// </summary>
        public DataTable ObtenerPrecuenta(string numPed, bool usarPrecioConIgv = false, bool incluirIGV0 = false)
        {
            if (string.IsNullOrWhiteSpace(numPed))
                throw new ArgumentException("numPed no puede ser vacío.", nameof(numPed));

            // 1) Base: SP dbo.Pedido
            var dt = _dao.ObtenerDesdePedido(numPed);

            // Si no hay filas, devolvemos dt tal cual (el RDLC se mostrará vacío o con header si lo configuras)
            if (dt.Rows.Count == 0)
                return dt;

            // 2) (Opcional) Merge con líneas IGV=0 si quieres mostrarlas
            if (incluirIGV0)
            {
                var falt = _dao.ObtenerLineasIGV0(numPed);
                if (falt.Rows.Count > 0)
                {
                    // Aseguramos columnas mínimas
                    EnsureColumn(dt, "num_item", typeof(object));
                    EnsureColumn(dt, "can_pprd", typeof(decimal));
                    EnsureColumn(dt, "pre_pprd", typeof(decimal));
                    EnsureColumn(dt, "imp_tprd", typeof(decimal));
                    EnsureColumn(dt, "pre_igv", typeof(decimal));
                    EnsureColumn(dt, "imp_igv", typeof(decimal));
                    EnsureColumn(dt, "cdg_prod", typeof(object));
                    EnsureColumn(dt, "des_prod", typeof(string));

                    foreach (DataRow r in falt.Rows)
                    {
                        var n = dt.NewRow();

                        // Copiamos columnas de detalle básicas
                        SafeCopy(r, n, "num_item", "can_pprd", "pre_pprd", "imp_tprd", "pre_igv", "imp_igv", "cdg_prod", "des_prod");

                        // Copiamos cabecera de la primera fila existente (num_ped, fec_ped, etc.)
                        var cab = dt.Rows[0];
                        foreach (DataColumn c in dt.Columns)
                            if (n[c.ColumnName] == DBNull.Value || n[c.ColumnName] == null)
                                n[c.ColumnName] = cab[c.ColumnName];

                        dt.Rows.Add(n);
                    }

                    // Orden por item
                    dt.DefaultView.Sort = "num_item ASC";
                    dt = dt.DefaultView.ToTable();
                }
            }

            // 3) Resolver nombre de ambiente (si solo viene cdg_area)
            EnsureColumn(dt, "des_ambiente", typeof(string));
            string desAmb = "";
            if (dt.Columns.Contains("cdg_area"))
            {
                string cdgArea = Convert.ToString(dt.Rows[0]["cdg_area"]);
                desAmb = string.IsNullOrWhiteSpace(cdgArea) ? "" : _dao.ObtenerNombreAmbiente(cdgArea, "ACJ");
            }
            foreach (DataRow r in dt.Rows) r["des_ambiente"] = desAmb;

            // 4) Precio y SubTotal según preferencia (con/sin IGV)
            EnsureColumn(dt, "Precio", typeof(decimal));
            EnsureColumn(dt, "SubTotal", typeof(decimal));
            foreach (DataRow r in dt.Rows)
            {
                decimal can = ToDec(r, "can_pprd");
                decimal pre = usarPrecioConIgv ? ToDec(r, "pre_igv") : ToDec(r, "pre_pprd");
                decimal sub = usarPrecioConIgv ? Math.Round(pre * can, 2) : ToDec(r, "imp_tprd");
                r["Precio"] = pre;
                r["SubTotal"] = sub;
            }

            // 5) CantidadTotalProductos para mostrar en el pie (usando First() en RDLC)
            EnsureColumn(dt, "CantidadTotalProductos", typeof(decimal));
            var totalCant = dt.AsEnumerable().Sum(x => ToDec(x, "can_pprd"));
            foreach (DataRow r in dt.Rows) r["CantidadTotalProductos"] = totalCant;

            dt.AcceptChanges();
            return dt;
        }

        // ---------- Helpers ----------

        private static void EnsureColumn(DataTable dt, string name, Type type)
        {
            if (!dt.Columns.Contains(name))
                dt.Columns.Add(name, type);
        }

        private static void SafeCopy(DataRow src, DataRow dst, params string[] cols)
        {
            foreach (var c in cols)
                if (src.Table.Columns.Contains(c) && dst.Table.Columns.Contains(c))
                    dst[c] = src[c];
        }

        private static decimal ToDec(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value || r[col] == null)
                return 0m;
            try { return Convert.ToDecimal(r[col]); }
            catch { return 0m; }
        }
    }
}
