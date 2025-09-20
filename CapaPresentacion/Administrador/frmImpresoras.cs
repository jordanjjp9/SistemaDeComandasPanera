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

      //  private const string LISTA_PRECIO_POR_DEFECTO = "001";

        public frmImpresoras()
        {
            InitializeComponent();
            ConfigurarGrid();
            WireEvents();
        }

        private void frmImpresoras_Load(object sender, EventArgs e)
        {
            CargarDatos();
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
                HeaderText = "CDG_PROD",
                DataPropertyName = "CDG_PROD",
                ReadOnly = true,
                FillWeight = 16
            };

            // 2) Descripción
            var c2 = new DataGridViewTextBoxColumn
            {
                Name = "DES_PROD",
                HeaderText = "DES_PROD",
                DataPropertyName = "DES_PROD",
                ReadOnly = true,
                FillWeight = 44
            };

            // 3) IMP_PROD (código de formato principal)
            var c3 = new DataGridViewTextBoxColumn
            {
                Name = "IMP_PROD",
                HeaderText = "IMP_PROD",
                DataPropertyName = "IMP_PROD",
                ReadOnly = true,
                FillWeight = 10
            };

            // 4) DES_FORM (principal) — encabezado igual que en tu captura
            var c4 = new DataGridViewTextBoxColumn
            {
                Name = "DES_FORM_PRN",
                HeaderText = "DES_FORM",
                DataPropertyName = "DES_FORM_PRN",
                ReadOnly = true,
                FillWeight = 20
            };

            // 5) CDG_IMP (código de formato secundario)
            var c5 = new DataGridViewTextBoxColumn
            {
                Name = "CDG_IMP",
                HeaderText = "CDG_IMP",
                DataPropertyName = "CDG_IMP",
                ReadOnly = true,
                FillWeight = 10
            };

            // 6) DES_FORM (secundaria) — encabezado igual que en tu captura
            var c6 = new DataGridViewTextBoxColumn
            {
                Name = "DES_FORM_SEC",
                HeaderText = "DES_FORM",
                DataPropertyName = "DES_FORM_SEC",
                ReadOnly = true,
                FillWeight = 20
            };

            dgvImpre.Columns.AddRange(new DataGridViewColumn[] { c1, c2, c3, c4, c5, c6 });
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

        private void CargarDatos()
        {
            var dt = _svc.ListarProductosConFormato(); // ← sin parámetros
            _bs.DataSource = dt;
            dgvImpre.DataSource = _bs;

            foreach (DataGridViewColumn c in dgvImpre.Columns)
                c.DefaultCellStyle.NullValue = "NULL";
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
                $"OR DES_PROD LIKE '%{esc}%' " +
                $"OR IMP_PROD LIKE '%{esc}%' " +
                $"OR DES_FORM_PRN LIKE '%{esc}%' " +
                $"OR CDG_IMP LIKE '%{esc}%' " +
                $"OR DES_FORM_SEC LIKE '%{esc}%'";
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
            // 1) Validaciones básicas
            if (dgvImpre.CurrentRow == null)
            {
                MessageBox.Show("Selecciona un producto.", "Impresoras", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string cdgProd = (dgvImpre.CurrentRow.Cells["CDG_PROD"].Value ?? "").ToString();
            if (string.IsNullOrWhiteSpace(cdgProd))
            {
                MessageBox.Show("No se pudo obtener el código del producto.", "Impresoras", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2) Diálogo de selección de impresora
            using (var dlg = new frmListImpresoras())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return;

                string cod = dlg.CodigoSeleccionado;   // p.ej. "004" o "" si se quiere quitar
                string nom = dlg.NombreSeleccionado;   // p.ej. "JUGUERIA" o "" si se quiere quitar

                // 3) Persistencia en BD (si cod vacío => dejar NULL)
                bool ok = string.IsNullOrWhiteSpace(cod)
                    ? _svc.QuitarImpresoraSecundaria(cdgProd)
                    : _svc.GuardarImpresoraSecundaria(cdgProd, cod);

                if (!ok)
                {
                    MessageBox.Show("No se pudo guardar la impresora secundaria.", "Impresoras", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // 4) Reflejar el cambio en la fila enlazada (sin recargar todo)
                if (_bs.Current is DataRowView rv)
                {
                    // Código de formato secundario
                    rv.Row["CDG_IMP"] = string.IsNullOrWhiteSpace(cod) ? (object)DBNull.Value : cod.PadLeft(3, '0');
                    // Descripción (nombre) del formato secundario
                    rv.Row["DES_FORM_SEC"] = string.IsNullOrWhiteSpace(nom) ? (object)DBNull.Value : nom.Trim();
                    rv.EndEdit();
                }

                // 5) Refresco visual y mantener selección
                dgvImpre.Refresh();
            }
        }
    }
}
