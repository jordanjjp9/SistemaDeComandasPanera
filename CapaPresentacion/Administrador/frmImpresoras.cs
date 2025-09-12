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

namespace CapaPresentacion.Administrador
{
    public partial class frmImpresoras : Form
    {
        private readonly cnImpresora _svc = new cnImpresora();
        private readonly BindingSource _bs = new BindingSource();
        private DataTable _dt;

        private const string LISTA_PRECIO_POR_DEFECTO = "001";

        public frmImpresoras()
        {
            InitializeComponent();
            ConfigurarGrid();
            WireEvents();
        }

        private void frmImpresoras_Load(object sender, EventArgs e)
        {
            CargarDatos(LISTA_PRECIO_POR_DEFECTO);
            txtProducto.Focus();


        }


        private void ConfigurarGrid()
        {
            dgvImpre.AutoGenerateColumns = false;
            dgvImpre.AllowUserToAddRows = false;
            dgvImpre.AllowUserToDeleteRows = false;
            dgvImpre.MultiSelect = false;
            dgvImpre.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvImpre.RowHeadersVisible = false;

            // Tamaño
            dgvImpre.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvImpre.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;

            // Encabezados
            dgvImpre.ColumnHeadersVisible = true;
            dgvImpre.EnableHeadersVisualStyles = false;
            dgvImpre.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvImpre.ColumnHeadersHeight = 32;
            dgvImpre.ColumnHeadersDefaultCellStyle.BackColor = SystemColors.Control;
            dgvImpre.ColumnHeadersDefaultCellStyle.ForeColor = Color.Black;
            dgvImpre.ColumnHeadersDefaultCellStyle.Font = new Font(dgvImpre.Font, FontStyle.Bold);

            // Celdas
            dgvImpre.DefaultCellStyle.WrapMode = DataGridViewTriState.False;

            // Limpiar columnas previas
            dgvImpre.Columns.Clear();

            // 1) Código
            var c1 = new DataGridViewTextBoxColumn
            {
                Name = "CDG_PROD",
                HeaderText = "Código",
                DataPropertyName = "CDG_PROD",     // alias del SELECT
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                FillWeight = 18
            };
            c1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // 2) Descripción
            var c2 = new DataGridViewTextBoxColumn
            {
                Name = "Producto",
                HeaderText = "Descripción",
                DataPropertyName = "Producto",     // alias del SELECT
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                FillWeight = 52
            };
            c2.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // 3) Imp. Princ (Nombre)
            var c3 = new DataGridViewTextBoxColumn
            {
                Name = "ImprePrin",
                HeaderText = "Imp. Princ (Nombre)",
                DataPropertyName = "ImprePrin",    // alias del SELECT
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                FillWeight = 15
            };
            c3.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // 4) Impresora Sec (Nombre)
            var c4 = new DataGridViewTextBoxColumn
            {
                Name = "ImpreSec",
                HeaderText = "Impresora Sec",
                DataPropertyName = "ImpreSec",     // alias del SELECT
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                FillWeight = 15
            };
            c4.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            dgvImpre.Columns.AddRange(new DataGridViewColumn[] { c1, c2, c3, c4 });
        }



        private void WireEvents()
        {
            Load += frmImpresoras_Load;

            txtProducto.TextChanged -= txtProducto_TextChanged;
            txtProducto.TextChanged += txtProducto_TextChanged;

            txtProducto.KeyDown -= txtProducto_KeyDown;
            txtProducto.KeyDown += txtProducto_KeyDown;

            btnAgregar.Click -= btnAgregar_Click;
            btnAgregar.Click += btnAgregar_Click;
        }

        private void CargarDatos(string cdgLprc)
        {
            _dt = _svc.ListarProductosGrid4(cdgLprc); // 👈 ESTA
            _bs.DataSource = _dt;
            dgvImpre.DataSource = _bs;
        }

        private void AplicarFiltro()
        {

            if (_bs.DataSource == null) return;

            string q = (txtProducto.Text ?? "").Trim();
            if (string.IsNullOrEmpty(q))
            {
                _bs.RemoveFilter();
                return;
            }

            string esc = q.Replace("'", "''");

            // Filtro sobre las 4 columnas visibles
            _bs.Filter =
                $"Convert(CDG_PROD,'System.String') LIKE '%{esc}%' " +
                $"OR Producto LIKE '%{esc}%' " +
                $"OR ImprePrin LIKE '%{esc}%' " +
                $"OR ImpreSec LIKE '%{esc}%'";
        }

        private void txtProducto_TextChanged(object sender, EventArgs e)
        {
            AplicarFiltro();
        }

        private void txtProducto_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                txtProducto.Clear();
                e.SuppressKeyPress = true;
            }
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {

            if (dgvImpre.CurrentRow == null)
            {
                MessageBox.Show("Selecciona un producto.");
                return;
            }

            string cdgProd = (dgvImpre.CurrentRow.Cells["CDG_PROD"].Value ?? "").ToString();
            if (string.IsNullOrWhiteSpace(cdgProd))
            {
                MessageBox.Show("No se pudo obtener el código del producto.");
                return;
            }

            using (var dlg = new frmListImpresoras())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;

                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                // Código (001/002/...) y nombre elegidos del catálogo M_FRMIMP
                string cod = dlg.CodigoSeleccionado;
                string nom = dlg.NombreSeleccionado;

                // Guardar en BD: M_PRODUC.CDG_IMP = cod
                bool ok = _svc.GuardarImpresoraSecundaria(cdgProd, cod);
                if (!ok)
                {
                    MessageBox.Show("No se pudo guardar la impresora.");
                    return;
                }

                // Reflejar en la UI: mostrar el NOMBRE en la columna visible ImpreSec
                dgvImpre.CurrentRow.Cells["ImpreSec"].Value = nom;
                dgvImpre.Refresh();
            }
        }
    }
}
