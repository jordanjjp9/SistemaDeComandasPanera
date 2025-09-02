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
    public partial class frmCJugoDesayuno : Form
    {
        public class SeleccionSimple
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioExtra { get; set; }
        }

        // ===== Dependencias / configuración =====
        private readonly cnProducto _svcProductos = new cnProducto();
        public string ListaPrecio { get; set; } = "001";

        // ===== Parámetros de entrada/salida =====
        public int CantidadRequerida { get; set; } = 1;
        public string Titulo { get; set; } = "Elige jugos del desayuno";

        // Texto que quieres ver arriba: "2 x DESAYUNO AMERICANO"
        public string ProductoBaseTexto { get; set; } = string.Empty;

        // Resultado
        public List<SeleccionSimple> Selecciones { get; private set; } = new List<SeleccionSimple>();

        // ★ NUEVO: devolverás también las notas para que el padre las use
        public string NotasJugo { get; private set; } = string.Empty;

        // Estado interno
        private int _seleccionados = 0;

        public frmCJugoDesayuno()
        {
            InitializeComponent();


            this.StartPosition = FormStartPosition.CenterParent;
            this.Load += frmCJugoDesayuno_Load;

            // Botones inferiores
            btnContinuar.Click += btnContinuar_Click;
            btnEliminar.Click += btnEliminar_Click;
            btnCerrar.Click += btnCerrar_Click;

            // Enganchar todos los botones de opciones (los de los jugos)
            WireOptionButtons(this);
        }
        private void frmCJugoDesayuno_Load(object sender, EventArgs e)
        {
            lblTitle.Text = Titulo;

            // pinta el desayuno que vino del principal
            if (!string.IsNullOrWhiteSpace(ProductoBaseTexto))
                txtProductoSelect.Text = ProductoBaseTexto;

            // limpiar el área de notas
            txtNotasJugoDes.Clear();

            ActualizarEstado();
        }
        private void WireOptionButtons(Control root)
        {
            if (root == null) return;

            bool esBoton = (root is Button) ||
                           (root.GetType().FullName ?? string.Empty)
                           .IndexOf("Guna2Button", StringComparison.OrdinalIgnoreCase) >= 0;

            // ★ EVITA enganchar los botones de control
            if (esBoton &&
                !string.Equals(root.Name, "btnContinuar", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(root.Name, "btnEliminar", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(root.Name, "btnCerrar", StringComparison.OrdinalIgnoreCase))
            {
                root.Click -= Opcion_Click;
                root.Click += Opcion_Click;
            }

            foreach (Control c in root.Controls)
                WireOptionButtons(c);
        }

        private void Opcion_Click(object sender, EventArgs e)
        {
            ////if (_seleccionados >= CantidadRequerida) return;

            ////Control ctrl = sender as Control;
            ////if (ctrl == null) return;

            ////SeleccionSimple opt = ParseOpcionFromControl(ctrl);
            ////if (opt == null) return;

            ////Selecciones.Add(opt);
            ////_seleccionados++;

            ////lstResumen.Items.Add(string.Format(
            ////    CultureInfo.InvariantCulture,
            ////    "{0} {1} (extra S/ {2:0.00})",
            ////    opt.Codigo, opt.Descripcion, opt.PrecioExtra));

            ////ActualizarEstado();

            if (_seleccionados >= CantidadRequerida) return;

            Control ctrl = sender as Control;
            if (ctrl == null) return;

            var opt = ParseOpcionFromControl(ctrl);   // ← viene con precio de BD si hay código
            if (opt == null) return;

            Selecciones.Add(opt);
            _seleccionados++;

            // Mostrar SOLO aquí (no usamos ListBox):
            AppendLineaNota(opt);

            ActualizarEstado();
        }
        private void AppendLineaNota(SeleccionSimple opt)
        {
            var sb = new StringBuilder();
            sb.Append("- ").Append(opt.Descripcion);
            if (opt.PrecioExtra > 0)
                sb.Append(" = S/ ").Append(opt.PrecioExtra.ToString("0.00", CultureInfo.InvariantCulture));

            if (txtNotasJugoDes.TextLength > 0)
                txtNotasJugoDes.AppendText(Environment.NewLine);

            txtNotasJugoDes.AppendText(sb.ToString());
            txtNotasJugoDes.SelectionStart = txtNotasJugoDes.TextLength;
            txtNotasJugoDes.ScrollToCaret();
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
            txtNotasJugoDes.Text = sb.ToString();
            txtNotasJugoDes.SelectionStart = txtNotasJugoDes.TextLength;
            txtNotasJugoDes.ScrollToCaret();
        }

        // Lee la opción desde Tag o (fallback) desde Name/Text
        private SeleccionSimple ParseOpcionFromControl(Control c)
        {
            //////// Tag como objeto SeleccionSimple
            //////SeleccionSimple opt = c.Tag as SeleccionSimple;
            //////if (opt != null) return Clonar(opt);

            //////// Tag como string: "CODIGO|Descripcion|Precio"
            //////string tagText = c.Tag as string;
            //////if (!string.IsNullOrEmpty(tagText))
            //////{
            //////    string[] parts = tagText.Split('|');
            //////    if (parts.Length >= 2)
            //////    {
            //////        string cod = parts[0].Trim();
            //////        string des = parts[1].Trim();
            //////        decimal precio = 0m;
            //////        if (parts.Length >= 3)
            //////            decimal.TryParse(parts[2].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out precio);

            //////        SeleccionSimple r = new SeleccionSimple();
            //////        r.Codigo = cod;
            //////        r.Descripcion = des;
            //////        r.PrecioExtra = precio;
            //////        return r;
            //////    }
            //////}

            //////// Fallback: usa Name/Text
            //////SeleccionSimple def = new SeleccionSimple();
            //////def.Codigo = (c.Name ?? "").Trim();
            //////def.Descripcion = (c.Text ?? "").Trim();
            //////def.PrecioExtra = 0m;
            //////return def;

            // 1) Tag como DTO
            var dto = c.Tag as SeleccionSimple;
            if (dto != null)
            {
                var r = new SeleccionSimple { Codigo = dto.Codigo, Descripcion = dto.Descripcion, PrecioExtra = dto.PrecioExtra };
                if (r.PrecioExtra == 0m && !string.IsNullOrWhiteSpace(r.Codigo))
                    r.PrecioExtra = ObtenerPrecioDeCodigo(r.Codigo);
                return r;
            }

            // 2) Tag string "COD|Desc|Precio?" (Precio opcional)
            var tag = c.Tag as string;
            if (!string.IsNullOrWhiteSpace(tag))
            {
                if (tag.Trim().Equals("SIN", StringComparison.OrdinalIgnoreCase))
                    return new SeleccionSimple { Codigo = "", Descripcion = (c.Text ?? "SIN JUGO").Trim(), PrecioExtra = 0m };

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

            // 3) Fallback: código desde Name "btnProd0000000232"
            string codigo = "";
            var m = Regex.Match(c.Name ?? "", @"\d+");
            if (m.Success) codigo = m.Value;

            // si no hay código y el texto dice SIN → 0
            if (string.IsNullOrWhiteSpace(codigo) &&
                (c.Text ?? "").IndexOf("SIN", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new SeleccionSimple { Codigo = "", Descripcion = (c.Text ?? "SIN JUGO").Trim(), PrecioExtra = 0m };
            }

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

        //////private SeleccionSimple Clonar(SeleccionSimple s)
        //////{
        //////    //SeleccionSimple n = new SeleccionSimple();
        //////    //n.Codigo = s.Codigo;
        //////    //n.Descripcion = s.Descripcion;
        //////    //n.PrecioExtra = s.PrecioExtra;
        //////    //return n;
        //////    return new SeleccionSimple { Codigo = s.Codigo, Descripcion = s.Descripcion, PrecioExtra = s.PrecioExtra };
        //////}

        private void ActualizarEstado()
        {
            ////int faltan = Math.Max(0, CantidadRequerida - _seleccionados);

            ////lblEstado.Text = string.Format(
            ////    CultureInfo.InvariantCulture,
            ////    "Seleccionados: {0}/{1}  •  Faltan: {2}",
            ////    _seleccionados, CantidadRequerida, faltan);

            ////btnContinuar.Enabled = (_seleccionados == CantidadRequerida);
            ////btnEliminar.Enabled = (_seleccionados > 0);

            int faltan = Math.Max(0, CantidadRequerida - _seleccionados);

            if (lblEstado != null)
                lblEstado.Text = string.Format(CultureInfo.InvariantCulture,
                    "Seleccionados: {0}/{1} • Faltan: {2}", _seleccionados, CantidadRequerida, faltan);

            btnContinuar.Enabled = (_seleccionados == CantidadRequerida);
            btnEliminar.Enabled = (_seleccionados > 0);
        }

        private void btnContinuar_Click(object sender, EventArgs e)
        {
            if (_seleccionados != CantidadRequerida) return;

            // ★ DEVOLVER las notas escritas
            NotasJugo = txtNotasJugoDes != null ? txtNotasJugoDes.Text : string.Empty;

            this.DialogResult = DialogResult.OK;
            this.Close();
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

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
