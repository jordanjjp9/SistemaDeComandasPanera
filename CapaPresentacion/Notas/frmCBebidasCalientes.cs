using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaNegocio;

namespace CapaPresentacion.Notas
{
    public partial class frmCBebidasCalientes : Form
    {
        public class SeleccionSimple
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioExtra { get; set; }
        }

        private readonly cnProducto _svcProductos = new cnProducto();
        public string ListaPrecio { get; set; } = "001";

        public int CantidadRequerida { get; set; } = 1;
        public string Titulo { get; set; } = "Bebidas Calientes";
        public string ProductoBaseTexto { get; set; } = string.Empty;

        public List<SeleccionSimple> Selecciones { get; private set; } = new List<SeleccionSimple>();

        private int _seleccionados = 0;

        // Controles que debes tener en el diseñador:
        // lblTitle, lblEstado, txtProductoSelect (readonly multiline),
        // txtNotasBCalient (multiline), btnContinuar, btnEliminar, btnCerrar (X)

        public frmCBebidasCalientes()
        {
            InitializeComponent();

            this.StartPosition = FormStartPosition.CenterParent;
            this.Load += frmCBebidasCalientes_Load;

            btnContinuar.Click += btnContinuar_Click;
            btnEliminar.Click += btnEliminar_Click;
            //btnCerrar.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; Close(); };

            WireOptionButtons(this);
        }

        private void frmCBebidasCalientes_Load(object sender, EventArgs e)
        {
            lblTitle.Text = Titulo;
            if (!string.IsNullOrWhiteSpace(ProductoBaseTexto))
                txtProductoSelect.Text = ProductoBaseTexto;

            txtNotasBCalient.Clear();
            ActualizarEstado();
        }
        private void WireOptionButtons(Control root)
        {
            if (root == null) return;
            bool esBoton = (root is Button) ||
                           ((root.GetType().FullName ?? "")
                              .IndexOf("Guna2Button", StringComparison.OrdinalIgnoreCase) >= 0);
            if (esBoton)
            {
                root.Click -= Opcion_Click;
                root.Click += Opcion_Click;
            }
            foreach (Control c in root.Controls)
                WireOptionButtons(c);
        }

        private void Opcion_Click(object sender, EventArgs e)
        {
            if (_seleccionados >= CantidadRequerida) return;
            var ctrl = sender as Control;
            if (ctrl == null) return;

            var opt = ParseOpcionFromControl(ctrl);
            if (opt == null) return;

            Selecciones.Add(opt);
            _seleccionados++;

            AppendLineaNota(opt);
            ActualizarEstado();
        }

        private void AppendLineaNota(SeleccionSimple opt)
        {
            var sb = new StringBuilder();
            sb.Append("- ").Append(opt.Descripcion);
            if (opt.PrecioExtra > 0)
                sb.Append(" = S/ ").Append(opt.PrecioExtra.ToString("0.00", CultureInfo.InvariantCulture));

            if (txtNotasBCalient.TextLength > 0)
                txtNotasBCalient.AppendText(Environment.NewLine);
            txtNotasBCalient.AppendText(sb.ToString());
            txtNotasBCalient.SelectionStart = txtNotasBCalient.TextLength;
            txtNotasBCalient.ScrollToCaret();
        }

        private void RebuildNotas()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < Selecciones.Count; i++)
            {
                var s = Selecciones[i];
                if (i > 0) sb.AppendLine();
                sb.Append("- ").Append(s.Descripcion);
                if (s.PrecioExtra > 0)
                    sb.Append(" = S/ ").Append(s.PrecioExtra.ToString("0.00", CultureInfo.InvariantCulture));
            }
            txtNotasBCalient.Text = sb.ToString();
            txtNotasBCalient.SelectionStart = txtNotasBCalient.TextLength;
            txtNotasBCalient.ScrollToCaret();
        }

        private SeleccionSimple ParseOpcionFromControl(Control c)
        {
            // 1) Tag como DTO
            var dto = c.Tag as SeleccionSimple;
            if (dto != null)
            {
                var r = new SeleccionSimple { Codigo = dto.Codigo, Descripcion = dto.Descripcion, PrecioExtra = dto.PrecioExtra };
                if (r.PrecioExtra == 0m && !string.IsNullOrWhiteSpace(r.Codigo))
                    r.PrecioExtra = ObtenerPrecioDeCodigo(r.Codigo);
                return r;
            }

            // 2) Tag como "COD|Desc|Precio?"
            var tag = c.Tag as string;
            if (!string.IsNullOrWhiteSpace(tag))
            {
                var parts = tag.Split('|');
                if (parts.Length >= 2)
                {
                    var cod = parts[0].Trim();
                    var des = parts[1].Trim();
                    decimal precio = 0m;
                    if (parts.Length >= 3)
                        decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out precio);
                    if (precio == 0m && !string.IsNullOrWhiteSpace(cod))
                        precio = ObtenerPrecioDeCodigo(cod);

                    return new SeleccionSimple { Codigo = cod, Descripcion = des, PrecioExtra = precio };
                }
            }

            // 3) Fallback: código desde Name
            string codigo = "";
            var m = Regex.Match(c.Name ?? "", @"\d+");
            if (m.Success) codigo = m.Value;

            return new SeleccionSimple
            {
                Codigo = codigo,
                Descripcion = (c.Text ?? "").Trim(),
                PrecioExtra = string.IsNullOrWhiteSpace(codigo) ? 0m : ObtenerPrecioDeCodigo(codigo)
            };
        }

        private decimal ObtenerPrecioDeCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return 0m;
            var cod10 = codigo.Trim().PadLeft(10, '0');
            try
            {
                var p = _svcProductos.Obtener(cod10, ListaPrecio);
                if (p == null) return 0m;
                return p.PrecioUnitario != 0m ? p.PrecioUnitario : p.ValorUnitario;
            }
            catch { return 0m; }
        }

        private void ActualizarEstado()
        {
            int faltan = Math.Max(0, CantidadRequerida - _seleccionados);
            if (lblEstado != null)
                lblEstado.Text = $"Seleccionados: {_seleccionados}/{CantidadRequerida} • Faltan: {faltan}";

            btnContinuar.Enabled = (_seleccionados == CantidadRequerida);
            btnEliminar.Enabled = (_seleccionados > 0);
        }

        private void btnContinuar_Click(object sender, EventArgs e)
        {
            if (_seleccionados != CantidadRequerida) return;
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (_seleccionados <= 0) return;
            var last = Selecciones.Count - 1;
            if (last >= 0)
            {
                Selecciones.RemoveAt(last);
                _seleccionados--;
                RebuildNotas();
                ActualizarEstado();
            }
        }
    }
}
