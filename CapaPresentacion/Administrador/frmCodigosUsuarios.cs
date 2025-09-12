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
        private bool _eventsWired;

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

            // CDG_VEND (solo lectura)
            var cVend = new DataGridViewTextBoxColumn
            {
                Name = "CDG_VEND",
                HeaderText = "CDG_VEND",
                DataPropertyName = "CDG_VEND",
                ReadOnly = true
            };

            // DES_VEND (solo lectura)
            var cDes = new DataGridViewTextBoxColumn
            {
                Name = "DES_VEND",
                HeaderText = "DES_VEND",
                DataPropertyName = "DES_VEND",
                ReadOnly = true
            };

            // CDG_USR (editable)
            var cUsr = new DataGridViewTextBoxColumn
            {
                Name = "CDG_USR",
                HeaderText = "CDG_USR",
                DataPropertyName = "CDG_USR",
                ReadOnly = false
            };
            cUsr.MaxInputLength = 4;

            // 🔁 REEMPLAZO: columna visible "Estado" mapeada a SWT_VEND (0/1)
            var cEstado = new DataGridViewTextBoxColumn
            {
                Name = "Estado",
                HeaderText = "Estado",
                DataPropertyName = "SWT_VEND", // viene 0/1 desde la BD
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.Automatic
            };

            dgvUsuarios.Columns.AddRange(new DataGridViewColumn[] { cVend, cDes, cUsr, cEstado });

            // Sólo CDG_USR editable
            dgvUsuarios.ReadOnly = false;
            foreach (DataGridViewColumn col in dgvUsuarios.Columns)
                col.ReadOnly = col.Name != "CDG_USR";
        }


        private void WireEvents()
        {
            //txtUsuarios.TextChanged += (_, __) => AplicarFiltro();
            //dgvUsuarios.EditingControlShowing += dgvUsuarios_EditingControlShowing;
            //dgvUsuarios.CellValidating += dgvUsuarios_CellValidating;

            if (_eventsWired) return;   // evita doble conexión si alguien llama de nuevo
            _eventsWired = true;

            txtUsuarios.TextChanged += (_, __) => AplicarFiltro();
            dgvUsuarios.EditingControlShowing += dgvUsuarios_EditingControlShowing;
            dgvUsuarios.CellValidating += dgvUsuarios_CellValidating;
            dgvUsuarios.CellFormatting += dgvUsuarios_CellFormatting;

            btnAgregar.Click -= btnAgregar_Click;   // ← por si ya estaba enganchado
            btnAgregar.Click += btnAgregar_Click;
        }

        private void CargarDatos()
        {
            //// Trae todos (activos e inactivos) y arma DataTable para poder filtrar con BindingSource
            //var lista = _svc.Listar(null, null) ?? new System.Collections.Generic.List<ceVendedor>();

            //_dt = new DataTable();
            //_dt.Columns.Add("CodSico", typeof(string));
            //_dt.Columns.Add("Nombre", typeof(string));
            //_dt.Columns.Add("CodSistema", typeof(string)); // columna libre del formulario
            //_dt.Columns.Add("Estado", typeof(string));     // "Activo"/"Inactivo"

            //foreach (var v in lista.OrderBy(x => x.Nombre))
            //    _dt.Rows.Add(v.Codigo, v.Nombre, string.Empty, v.Activo ? "Activo" : "Inactivo");

            //_bs.DataSource = _dt;
            //dgvUsuarios.DataSource = _bs;
            // Devuelve: CDG_VEND, DES_VEND, CDG_USR, SWT_VEND (int 0/1)
            _dt = _svc.ListarTablaParaUsuarios(filtro: null, soloActivos: null);
            _bs.DataSource = _dt;
            dgvUsuarios.DataSource = _bs;
        }
        private void AplicarFiltro()
        {
            //if (_bs.DataSource == null) return;
            //string q = (txtUsuarios.Text ?? "").Trim();

            //if (string.IsNullOrEmpty(q))
            //{
            //    _bs.RemoveFilter();
            //    return;
            //}

            //// escapamos comillas simples para RowFilter
            //string esc = q.Replace("'", "''");
            //_bs.Filter = $"CodSico LIKE '%{esc}%' OR Nombre LIKE '%{esc}%'";
            if (_bs.DataSource == null) return;
            string q = (txtUsuarios.Text ?? "").Trim();

            if (string.IsNullOrEmpty(q))
            {
                _bs.RemoveFilter();
                return;
            }

            string esc = q.Replace("'", "''");
            // Filtramos por columnas de texto/char
            _bs.Filter = $"CDG_VEND LIKE '%{esc}%' OR DES_VEND LIKE '%{esc}%' OR CDG_USR LIKE '%{esc}%'";
        }
        private void frmCodigosUsuarios_Load(object sender, EventArgs e)
        {
            CargarDatos();
            txtUsuarios.Focus();
        }

        private void btnAgregar_Click(object sender, EventArgs e)
        {
            //if (dgvUsuarios.CurrentRow == null)
            //{
            //    MessageBox.Show("Selecciona primero un vendedor.", "CodSistema", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //using (var dlg = new frmCodigoUser())
            //{
            //    dlg.StartPosition = FormStartPosition.CenterParent;
            //    if (dlg.ShowDialog(this) == DialogResult.OK)
            //    {
            //        var codigo = dlg.CodigoIngresado ?? string.Empty;
            //        dgvUsuarios.CurrentRow.Cells["CodSistema"].Value = codigo;
            //        dgvUsuarios.Refresh();
            //    }
            //}
            if (dgvUsuarios.CurrentRow == null)
            {
                MessageBox.Show("Selecciona primero un registro.", "CDG_USR",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 1) Claves de la fila seleccionada
            string cdgVend = (dgvUsuarios.CurrentRow.Cells["CDG_VEND"].Value ?? "").ToString().Trim();
            string actualUsr = (dgvUsuarios.CurrentRow.Cells["CDG_USR"].Value ?? "").ToString().Trim();

            if (string.IsNullOrEmpty(cdgVend))
            {
                MessageBox.Show("No se pudo obtener el CDG_VEND de la fila seleccionada.", "CDG_USR",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2) Diálogo para capturar el nuevo CDG_USR (4 dígitos)
            using (var dlg = new frmCodigoUser { CodigoInicial = actualUsr })
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                string nuevoUsr = dlg.CodigoIngresado ?? string.Empty; // ya validado: 4 dígitos

                // 3) Negocio → DAO → BD
                var res = _svc.ActualizarUsrPorVend(cdgVend, nuevoUsr);
                if (!res.Ok)
                {
                    MessageBox.Show(res.Motivo ?? "No se pudo actualizar el CDG_USR.", "CDG_USR",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 4) Reflejar en la UI (sin recargar todo)
                dgvUsuarios.CurrentRow.Cells["CDG_USR"].Value = nuevoUsr;
                dgvUsuarios.Refresh();

                MessageBox.Show("CDG_USR actualizado correctamente.", "CDG_USR",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        

        private void frmCodigosUsuarios_Shown(object sender, EventArgs e)
        {
            CargarDatos();
            txtUsuarios.Focus();
        }

        private void dgvUsuarios_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            //if (dgvUsuarios.CurrentCell?.OwningColumn?.Name == "CodSistema" && e.Control is TextBox tb)
            //{
            //    tb.MaxLength = 4;
            //    tb.KeyPress -= SoloDigitos_KeyPress;
            //    tb.KeyPress += SoloDigitos_KeyPress;
            //}
            if (dgvUsuarios.CurrentCell?.OwningColumn?.Name == "CDG_USR" && e.Control is TextBox tb)
            {
                tb.MaxLength = 4;
                tb.KeyPress -= SoloDigitos_KeyPress;
                tb.KeyPress += SoloDigitos_KeyPress;
            }
        }
        private void SoloDigitos_KeyPress(object sender, KeyPressEventArgs e)
        {
            //if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            //    e.Handled = true;
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void dgvUsuarios_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            //if (dgvUsuarios.Columns[e.ColumnIndex].Name != "CodSistema") return;

            //string v = (e.FormattedValue ?? "").ToString().Trim();

            //if (v.Length == 0)
            //{
            //    dgvUsuarios.Rows[e.RowIndex].ErrorText = string.Empty; // vacío permitido
            //    return;
            //}

            //bool ok = v.Length == 4 && v.All(char.IsDigit);
            //if (!ok)
            //{
            //    dgvUsuarios.Rows[e.RowIndex].ErrorText = "CodSistema debe ser exactamente 4 dígitos numéricos.";
            //    e.Cancel = true;
            //}
            //else
            //{
            //    dgvUsuarios.Rows[e.RowIndex].ErrorText = string.Empty;
            //}
            if (dgvUsuarios.Columns[e.ColumnIndex].Name != "CDG_USR") return;

            string v = (e.FormattedValue ?? "").ToString().Trim();

            // Permitimos vacío o 4 dígitos exactos
            if (v.Length == 0)
            {
                dgvUsuarios.Rows[e.RowIndex].ErrorText = string.Empty;
                return;
            }

            bool ok = v.Length == 4 && v.All(char.IsDigit);
            if (!ok)
            {
                dgvUsuarios.Rows[e.RowIndex].ErrorText = "CDG_USR debe ser exactamente 4 dígitos numéricos (o dejar vacío).";
                e.Cancel = true;
            }
            else
            {
                dgvUsuarios.Rows[e.RowIndex].ErrorText = string.Empty;
            }
        }

        private void dgvUsuarios_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvUsuarios.Columns[e.ColumnIndex].Name == "Estado" && e.Value != null && e.Value != DBNull.Value)
            {
                if (e.Value is int iv)
                {
                    e.Value = (iv == 1) ? "Activo" : "Inactivo";
                    e.FormattingApplied = true;
                }
                else
                {
                    // Por si llega como string
                    if (int.TryParse(e.Value.ToString(), out var v))
                    {
                        e.Value = (v == 1) ? "Activo" : "Inactivo";
                        e.FormattingApplied = true;
                    }
                }
            }
        }
    }
}
