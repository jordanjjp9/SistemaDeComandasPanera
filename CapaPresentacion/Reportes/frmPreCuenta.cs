using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaNegocio;
using Microsoft.Reporting.WinForms;

namespace CapaPresentacion.Reportes
{
    public partial class frmPreCuenta : Form
    {
        private readonly string _numPed;
        private readonly bool _usarPrecioConIgv;
        private readonly bool _incluirIGV0;

        public frmPreCuenta(string numPed, bool usarPrecioConIgv = false, bool incluirIGV0 = false)
        {
            InitializeComponent();
            _numPed = numPed;
            _usarPrecioConIgv = usarPrecioConIgv;
            _incluirIGV0 = incluirIGV0;
        }

        private void frmPreCuenta_Load(object sender, EventArgs e)
        {
            try
            {
                // 1) Trae el DataTable “plano” (usa precio con IGV) y limpia strings (trim)
                var svc = new cnPrecuenta();
                DataTable dtFull = svc.ObtenerPrecuenta(
                    _numPed,
                    usarPrecioConIgv: true,          // << fuerza precio de venta (con IGV)
                    incluirIGV0: _incluirIGV0);

                TrimAllStringColumns(dtFull);         // << elimina espacios en blanco al inicio/fin

                // 2) Construye el DataSet tipado (dsPrecuenta: dtCabecera + dtDetalle)
                var ds = new dsPrecuenta();

                // ---- CABECERA (1 fila) ----
                if (dtFull != null && dtFull.Rows.Count > 0)
                {
                    var r0 = dtFull.Rows[0];
                    var cab = ds.dtCabecera.NewdtCabeceraRow();

                    // Fecha solo dd/MM/yyyy; hora tal cual
                    cab.fec_ped = SafeDate(r0["fec_ped"]).ToString("dd/MM/yyyy");
                    cab.hra_ped = Convert.ToString(r0["hra_ped"]);
                    cab.num_ped = Convert.ToString(r0["num_ped"]);
                    cab.cdg_caja = Convert.ToString(r0["cdg_caja"]);
                    cab.des_vend = Convert.ToString(r0["des_vend"]);
                    cab.des_ambiente = Convert.ToString(r0["des_ambiente"]);
                    cab.num_mesa = Convert.ToString(r0["num_mesa"]);

                    cab.imp_ttot = ToDec(r0, "imp_ttot");
                    cab.CantidadTotalProductos = ToDec(r0, "CantidadTotalProductos");

                    ds.dtCabecera.AdddtCabeceraRow(cab);
                }

                // ---- DETALLE (n filas) ----
                if (dtFull != null)
                {
                    foreach (DataRow r in dtFull.Rows)
                    {
                        var det = ds.dtDetalle.NewdtDetalleRow();

                        det.num_item = ToInt(r, "num_item");          // orden
                        det.can_pprd = ToInt(r, "can_pprd");          // cantidad como entero
                        det.des_prod = Convert.ToString(r["des_prod"]);

                        var precioConIgv = ToDec(r, "pre_igv");       // precio de venta (incluye IGV)
                        det.Precio = precioConIgv;
                        det.SubTotal = Math.Round(precioConIgv * det.can_pprd, 2);

                        ds.dtDetalle.AdddtDetalleRow(det);
                    }
                }

                // 3) Vincula al ReportViewer (RDLC embebido)
                rvPrecuenta.Reset();
                rvPrecuenta.ProcessingMode = ProcessingMode.Local;
                rvPrecuenta.LocalReport.ReportEmbeddedResource =
                    "CapaPresentacion.Reportes.rvPrecuenta.rdlc"; // ajusta si tu archivo se llama distinto

                rvPrecuenta.LocalReport.DataSources.Clear();
                rvPrecuenta.LocalReport.DataSources.Add(
                    new ReportDataSource("dsCabecera", (DataTable)ds.dtCabecera));
                rvPrecuenta.LocalReport.DataSources.Add(
                    new ReportDataSource("dsDetalle", (DataTable)ds.dtDetalle));

                rvPrecuenta.RefreshReport();

                // (opcional) Para exportar a PDF automáticamente en vez de previsualizar:
                // ExportarPdfYAbrir(rvPrecuenta, $"Precuenta_{_numPed}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar la precuenta: " + ex.Message,
                    "Reporte", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private static DateTime SafeDate(object v)
        {
            if (v == null || v == DBNull.Value) return DateTime.MinValue;
            DateTime dt;
            return DateTime.TryParse(v.ToString(), out dt) ? dt : DateTime.MinValue;
        }
        private static decimal ToDec(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return 0m;
            decimal d; return decimal.TryParse(r[col].ToString(), out d) ? d : 0m;
        }
        private static int ToInt(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return 0;
            int i; return int.TryParse(r[col].ToString(), out i) ? i : (int)Math.Round(ToDec(r, col), 0);
        }
        private static void TrimAllStringColumns(DataTable dt)
        {
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    if (col.DataType == typeof(string))
                    {
                        var s = row[col] as string;
                        if (s != null) row[col] = s.Trim(); // quita espacios a los lados
                    }
                }
            }
        }
    }
}
