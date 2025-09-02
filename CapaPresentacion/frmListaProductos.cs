using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using CapaDatos;
using CapaEntidad;


namespace CapaPresentacion
{
    public partial class frmListaProductos : Form
    {
        private readonly DAOProductos _dao = new DAOProductos();
        private DataTable _tabla;
        public string SelectedCodigo { get; private set; }

        [DllImport("user32.dll")] private static extern bool ReleaseCapture();
        [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        public frmListaProductos()
        {
            InitializeComponent();
            pnlCabecera.MouseDown += pnlCabecera_MouseDown;
            this.Load += frmListaProductos_Load;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void pnlCabecera_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void frmListaProductos_Load(object sender, EventArgs e)
        {
            CargarDatos();
            ConfigurarGrid();

            dgvListPrd.MultiSelect = false;
            dgvListPrd.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvListPrd.ReadOnly = true;

            // Eventos para aceptar con Enter o doble clic
            dgvListPrd.KeyDown += dgvListPrd_KeyDown;
            dgvListPrd.CellDoubleClick += dgvListPrd_CellDoubleClick;
        }

        private void CargarDatos()
        {
            _tabla = _dao.ListarParaVenta();
            dgvListPrd.AutoGenerateColumns = false;
            dgvListPrd.DataSource = _tabla;
        }

        private void AddTextCol(string dataProperty, string header, bool readOnly = true,
                        DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleLeft,
                        string format = "",
                        DataGridViewAutoSizeColumnMode autoSize = DataGridViewAutoSizeColumnMode.AllCells)
        {
            var col = new DataGridViewTextBoxColumn
            {
                DataPropertyName = dataProperty,
                HeaderText = header,
                ReadOnly = readOnly,
                AutoSizeMode = autoSize
            };

            col.DefaultCellStyle.Alignment = align;

            if (!string.IsNullOrEmpty(format))
                col.DefaultCellStyle.Format = format;

            dgvListPrd.Columns.Add(col);
        }

        private void ConfigurarGrid()
        {
            dgvListPrd.Columns.Clear();
            dgvListPrd.AutoGenerateColumns = false;
            dgvListPrd.AllowUserToAddRows = false;
            dgvListPrd.RowHeadersVisible = false;
            dgvListPrd.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            // Columna Código (se ajusta al texto)
            AddTextCol("CDG_PROD", "Código", true, DataGridViewContentAlignment.MiddleLeft, "", DataGridViewAutoSizeColumnMode.AllCells);

            // Columna Producto (ocupa todo el espacio sobrante)
            AddTextCol("PRODUCTO", "Producto", true, DataGridViewContentAlignment.MiddleLeft, "", DataGridViewAutoSizeColumnMode.Fill);

            // Columna UM (se ajusta al texto)
            AddTextCol("UNM", "UM", true, DataGridViewContentAlignment.MiddleCenter, "", DataGridViewAutoSizeColumnMode.AllCells);

            // Columna Precio (ajuste al contenido)
            AddTextCol("PRECIO", "Precio", true, DataGridViewContentAlignment.MiddleRight, "N2", DataGridViewAutoSizeColumnMode.AllCells);

            // Columna Cantidad (ajuste al contenido, editable)
        }

        private void dgvListPrd_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var col = dgvListPrd.Columns[e.ColumnIndex].DataPropertyName;
            if (col == "CANTIDAD")
            {
                var cell = dgvListPrd.Rows[e.RowIndex].Cells[e.ColumnIndex];
                if (!decimal.TryParse(Convert.ToString(cell.Value), out var cant) || cant < 0)
                {
                    cell.Value = 0m;
                }
                // TOTAL se actualiza solo por la expresión del DataTable
                dgvListPrd.InvalidateRow(e.RowIndex);
            }
        }

        private void txtBusqProd_TextChanged(object sender, EventArgs e)
        {
            if (_tabla == null) return;

            var dv = _tabla.DefaultView;
            var t = (txtBusqProd.Text ?? "").Trim();

            // Escapar comillas simples para no romper el RowFilter
            var safe = t.Replace("'", "''");

            if (string.IsNullOrWhiteSpace(safe))
            {
                dv.RowFilter = "";
            }
            else
            {
                // Filtra por PRODUCTO o por CDG_PROD (case-insensitive aproximado)
                dv.RowFilter =
                    $"PRODUCTO LIKE '%{safe}%' OR CONVERT(CDG_PROD, 'System.String') LIKE '%{safe}%'";
                // Si PRODUCTO puede venir con espacios en BD, podrías usar: 
                // dv.RowFilter = $"LTRIM(RTRIM(PRODUCTO)) LIKE '%{safe}%' OR CONVERT(CDG_PROD, 'System.String') LIKE '%{safe}%'";
            }
        }

        private void dgvListPrd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true; // evita el beep y el avance de fila
                AceptarSeleccionActual();
            }
        }

        private void dgvListPrd_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
                AceptarSeleccionActual();
        }

        private void AceptarSeleccionActual()
        {
            if (dgvListPrd.CurrentRow == null) return;

            string cod = GetCodigoFilaActual();
            if (!string.IsNullOrWhiteSpace(cod))
            {
                SelectedCodigo = cod.Trim();
                this.DialogResult = DialogResult.OK; // cierra el modal
            }
        }

        private string GetCodigoFilaActual()
        {
            var row = dgvListPrd.CurrentRow;
            if (row == null) return null;

            if (row.DataBoundItem is ceProductos p && !string.IsNullOrWhiteSpace(p.Codigo))
                return p.Codigo;

            if (row.DataBoundItem is DataRowView drv)
            {
                string[] posibles = { "CDG_PROD", "Codigo", "Código", "CODIGO" };
                foreach (var name in posibles)
                    if (drv.Row.Table.Columns.Contains(name))
                        return Convert.ToString(drv[name]);
            }

            foreach (DataGridViewColumn c in dgvListPrd.Columns)
            {
                if (string.Equals(c.DataPropertyName, "CDG_PROD", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(c.DataPropertyName, "Codigo", StringComparison.OrdinalIgnoreCase))
                {
                    return Convert.ToString(row.Cells[c.Index].Value);
                }
            }

            return Convert.ToString(row.Cells[0].Value);
        }

    }
}
