using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaEntidad;
using CapaNegocio;

namespace CapaPresentacion.Administrador
{
    public partial class frmCodigosUsuarios : Form
    {
        private readonly cnVendedor _svc = new cnVendedor();
        private DataTable _dt;              // tabla con columnas: CodSico, Nombre, CodSistema, Estado
        private readonly BindingSource _bs = new BindingSource();

        public frmCodigosUsuarios()
        {
            InitializeComponent();
            Load += frmCodigosUsuarios_Load;

            ConfigurarGrid();
            WireEvents();

            CargarDatos();
        }
        private void ConfigurarGrid()
        {
            dgvUsuarios.AutoGenerateColumns = false;
            dgvUsuarios.ColumnHeadersVisible = true;
            dgvUsuarios.AllowUserToAddRows = false;
            dgvUsuarios.AllowUserToDeleteRows = false;
            dgvUsuarios.MultiSelect = false;
            dgvUsuarios.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUsuarios.RowHeadersVisible = false;
            dgvUsuarios.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dgvUsuarios.Columns.Clear();

            // CodSico
            var c1 = new DataGridViewTextBoxColumn
            {
                Name = "CodSico",
                HeaderText = "CodSico",
                DataPropertyName = "CodSico",
                ReadOnly = true
            };
            // Nombre
            var c2 = new DataGridViewTextBoxColumn
            {
                Name = "Nombre",
                HeaderText = "Nombre",
                DataPropertyName = "Nombre",
                ReadOnly = true
            };
            // CodSistema (editable)
            var c3 = new DataGridViewTextBoxColumn
            {
                Name = "CodSistema",
                HeaderText = "CodSistema",
                DataPropertyName = "CodSistema",
                ReadOnly = false
            };
            c3.MaxInputLength = 4; // 4 dígitos

            // Estado
            var c4 = new DataGridViewTextBoxColumn
            {
                Name = "Estado",
                HeaderText = "Estado Vendedor",
                DataPropertyName = "Estado",
                ReadOnly = true
            };

            dgvUsuarios.Columns.AddRange(new DataGridViewColumn[] { c1, c2, c3, c4 });

            // Asegura que solo CodSistema sea editable
            dgvUsuarios.ReadOnly = false;
            foreach (DataGridViewColumn col in dgvUsuarios.Columns)
                col.ReadOnly = col.Name != "CodSistema";
        }
        private void WireEvents()
        {
            txtUsuarios.TextChanged += (_, __) => AplicarFiltro();
            dgvUsuarios.EditingControlShowing += dgvUsuarios_EditingControlShowing;
            dgvUsuarios.CellValidating += dgvUsuarios_CellValidating;
        }

        private void CargarDatos()
        {
            // Trae todos (activos e inactivos) y arma DataTable para poder filtrar con BindingSource
            var lista = _svc.Listar(null, null) ?? new System.Collections.Generic.List<ceVendedor>();

            _dt = new DataTable();
            _dt.Columns.Add("CodSico", typeof(string));
            _dt.Columns.Add("Nombre", typeof(string));
            _dt.Columns.Add("CodSistema", typeof(string)); // columna libre del formulario
            _dt.Columns.Add("Estado", typeof(string));     // "Activo"/"Inactivo"

            foreach (var v in lista.OrderBy(x => x.Nombre))
                _dt.Rows.Add(v.Codigo, v.Nombre, string.Empty, v.Activo ? "Activo" : "Inactivo");

            _bs.DataSource = _dt;
            dgvUsuarios.DataSource = _bs;
        }
        private void AplicarFiltro()
        {
            if (_bs.DataSource == null) return;
            string q = (txtUsuarios.Text ?? "").Trim();

            if (string.IsNullOrEmpty(q))
            {
                _bs.RemoveFilter();
                return;
            }

            // escapamos comillas simples para RowFilter
            string esc = q.Replace("'", "''");
            _bs.Filter = $"CodSico LIKE '%{esc}%' OR Nombre LIKE '%{esc}%'";
        }
        private void frmCodigosUsuarios_Load(object sender, EventArgs e)
        {

        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            if (dgvUsuarios.CurrentRow == null)
            {
                MessageBox.Show("Selecciona primero un vendedor.", "CodSistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new frmCodigoUser())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    var codigo = dlg.CodigoIngresado ?? string.Empty;
                    dgvUsuarios.CurrentRow.Cells["CodSistema"].Value = codigo;
                    dgvUsuarios.Refresh();
                }
            }
        }
        

        private void frmCodigosUsuarios_Shown(object sender, EventArgs e)
        {
            CargarDatos();
            txtUsuarios.Focus();
        }

        private void dgvUsuarios_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dgvUsuarios.CurrentCell?.OwningColumn?.Name == "CodSistema" && e.Control is TextBox tb)
            {
                tb.MaxLength = 4;
                tb.KeyPress -= SoloDigitos_KeyPress;
                tb.KeyPress += SoloDigitos_KeyPress;
            }
        }
        private void SoloDigitos_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void dgvUsuarios_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (dgvUsuarios.Columns[e.ColumnIndex].Name != "CodSistema") return;

            string v = (e.FormattedValue ?? "").ToString().Trim();

            if (v.Length == 0)
            {
                dgvUsuarios.Rows[e.RowIndex].ErrorText = string.Empty; // vacío permitido
                return;
            }

            bool ok = v.Length == 4 && v.All(char.IsDigit);
            if (!ok)
            {
                dgvUsuarios.Rows[e.RowIndex].ErrorText = "CodSistema debe ser exactamente 4 dígitos numéricos.";
                e.Cancel = true;
            }
            else
            {
                dgvUsuarios.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

    }
}
