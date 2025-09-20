using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Text;
using Guna.UI2.WinForms;
using CapaPresentacion.Helpers;
using CapaEntidad;

namespace CapaPresentacion.Controles
{
    public partial class LineaPedidoItem : UserControl, ILineaSeleccionable
    {
        public DetalleRef RefDetalle { get; private set; }

        public void SetRefDetalle(DetalleRef r) => RefDetalle = r;
        public DetalleRef GetRefDetalle() => RefDetalle;
        ////private DetalleRef _refDetalle;
        ////public void SetRefDetalle(DetalleRef r) => _refDetalle = r;
        ////public DetalleRef GetRefDetalle() => _refDetalle;
        ////public bool TieneRefDetalle => _refDetalle != null;

        // public void SetRefDetalle(DetalleRef r) => RefDetalle = r;
        //public bool EsAntiguo => RefDetalle != null;
        /// <summary>
        /// /-------------------------------------//
        /// </summary>
        public string Codigo { get; private set; } = string.Empty;
        public string Descripcion { get; private set; } = string.Empty;
        public int Cantidad { get; private set; } = 1;
        public decimal PrecioUnitario { get; private set; } = 0m;
        public decimal Importe => Cantidad * PrecioUnitario;

        public string Notas { get; private set; } = string.Empty;

        private readonly ToolTip _tt = new ToolTip();
        private bool _pendingGrow = false;

        // colores para indicar selección sin cambiar tamaño
        private Color _fillNormal;
        private Color _fillSelected = Color.FromArgb(229, 244, 255);
        private Color _borderNormal;
        private Color _borderSelected = Color.FromArgb(94, 148, 255);

        public Control View => this;

        public LineaPedidoItem()
        {
            InitializeComponent();

            // ——— visual del textbox principal ———
            txtProducto.Multiline = true;
            txtProducto.WordWrap = true;
            txtProducto.AcceptsReturn = true;
            txtProducto.ScrollBars = ScrollBars.None;
            txtProducto.ReadOnly = true;
            txtProducto.Enabled = true;
            txtProducto.Cursor = Cursors.Hand;
            txtProducto.Dock = DockStyle.Top;
            txtProducto.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
            txtProducto.AutoSize = false;
            txtProducto.MinimumSize = new Size(10, 36);
            txtProducto.Padding = new Padding(6, 6, 6, 8); // ayuda al cálculo

            // guardo colores originales para “des-seleccionar”
            _fillNormal = txtProducto.FillColor;
            _borderNormal = txtProducto.BorderColor;

            // ——— eventos para recalcular ———
            Load += (s, e) => RequestAutoGrow();
            txtProducto.TextChanged += (s, e) => RequestAutoGrow();
            txtProducto.GotFocus += (s, e) => RequestAutoGrow();
            txtProducto.LostFocus += (s, e) => RequestAutoGrow();
            SizeChanged += (s, e) => RequestAutoGrow();

            // selección global al hacer click
            WireSelectClick(this);
            var inner = TryGetInnerTextBox(txtProducto);
            if (inner != null)
            {
                inner.Click -= Any_Click_Select; inner.Click += Any_Click_Select;
                inner.MouseDown -= Any_Click_Select; inner.MouseDown += Any_Click_Select;
            }

            // este control nunca cambia su BorderStyle (evita “saltos”)
            this.BorderStyle = BorderStyle.None;
            this.Margin = new Padding(6);
            this.MinimumSize = new Size(150, 40);
        }

        // ILineaSeleccionable – no toco BorderStyle
        public void SetVisualSelected(bool sel)
        {
            txtProducto.FillColor = sel ? _fillSelected : _fillNormal;
            txtProducto.BorderColor = sel ? _borderSelected : _borderNormal;
            txtProducto.BorderThickness = 1; // constante
        }

        // ===== API =====
        public void Configurar(string codigo, string descripcion, int cantidad, decimal precioUnitario, string notasIniciales)
        {
            Codigo = (codigo ?? string.Empty).Trim();
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? Codigo : descripcion.Trim();
            Cantidad = Math.Max(1, cantidad);
            PrecioUnitario = Math.Max(0m, precioUnitario);

            SetNotas(notasIniciales);
            RedibujarTextoConNotas();

            _tt.SetToolTip(txtProducto, "PU: S/ " + PrecioUnitario.ToString("0.00"));
        }

        public void SetNotas(string notas)
        {
            Notas = NormalizarNotas(notas);
            RedibujarTextoConNotas();
        }

        public void AppendNotas(string notas)
        {
            var extra = NormalizarNotas(notas);
            if (string.IsNullOrEmpty(extra)) return;
            Notas = string.IsNullOrEmpty(Notas) ? extra : (Notas + Environment.NewLine + extra);
            RedibujarTextoConNotas();
        }

        public string GetNotasRaw() => Notas ?? string.Empty;

        private static string NormalizarNotas(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var lines = (raw ?? string.Empty)
                        .Replace("\r\n", "\n").Replace("\r", "\n")
                        .Split('\n');
            var sb = new StringBuilder();
            foreach (var l in lines)
            {
                var s = (l ?? string.Empty).Trim();
                if (s.Length == 0) continue;
                if (!s.StartsWith("-")) s = "- " + s;
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(s);
            }
            return sb.ToString();
        }

        private void RedibujarTextoConNotas()
        {
            string header = $"{Cantidad} x {Descripcion.ToUpperInvariant()} = S/ {Importe:0.00}";
            txtProducto.Text = string.IsNullOrEmpty(Notas) ? header : header + Environment.NewLine + Notas;

            try { BeginInvoke((Action)RecalcAutoGrow); } catch { }
        }

        // ===== selección global =====
        private void WireSelectClick(Control root)
        {
            root.Click -= Any_Click_Select; root.Click += Any_Click_Select;
            root.MouseDown -= Any_Click_Select; root.MouseDown += Any_Click_Select;
            foreach (Control c in root.Controls) WireSelectClick(c);
        }
        private void Any_Click_Select(object s, EventArgs e) =>
            LineaSelection.Select(this, true);

        // ===== AutoGrow =====
        private void RequestAutoGrow()
        {
            if (!IsHandleCreated) { _pendingGrow = true; return; }
            try { BeginInvoke((Action)RecalcAutoGrow); }
            catch (InvalidOperationException) { _pendingGrow = true; }
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_pendingGrow) { _pendingGrow = false; RequestAutoGrow(); }
        }

        private void RecalcAutoGrow()
        {
            if (!IsHandleCreated) return;

            int hTxt = AltoNecesario(txtProducto);

            SuspendLayout();
            if (txtProducto.Height != hTxt)
                txtProducto.Height = hTxt;

            // alto total del control según bottom del textbox
            int nuevoAlto = txtProducto.Bottom + this.Padding.Bottom;
            if (Height != nuevoAlto)
                Height = nuevoAlto;
            ResumeLayout();
        }

        private static int AltoNecesario(Guna2TextBox tb)
        {
            if (tb == null) return 0;

            // base mínima
            int min = Math.Max(36, tb.Font.Height + 14);

            string text = tb.Text ?? string.Empty;
            if (text.Length == 0) return min;

            // 1) intentar con el TextBox interno de Guna para mayor precisión
            var inner = TryGetInnerTextBox(tb);
            if (inner != null)
            {
                try
                {
                    inner.Multiline = true;
                    inner.WordWrap = true;
                    inner.ScrollBars = ScrollBars.None;

                    int last = text.Length - 1;
                    while (last >= 0 && (text[last] == '\r' || text[last] == '\n')) last--;
                    if (last < 0) last = 0;

                    var pt = inner.GetPositionFromCharIndex(last);
                    // margen generoso por paddings/border Guna
                    int needed = pt.Y + inner.Font.Height + 22;
                    return Math.Max(min, needed);
                }
                catch { /* fall back */ }
            }

            // 2) fallback robusto con TextRenderer
            int w = Math.Max(1, tb.ClientSize.Width - 8);
            var size = TextRenderer.MeasureText(
                text + "\nA",
                tb.Font,
                new Size(w, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl | TextFormatFlags.NoPadding
            );
            // sumar holgura por paddings de Guna
            return Math.Max(min, size.Height + 24);
        }

        private static TextBox TryGetInnerTextBox(Control guna2TextBox)
        {
            try
            {
                var prop = guna2TextBox.GetType().GetProperty(
                    "TextBox", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(guna2TextBox, null) as TextBox;
            }
            catch { return null; }
        }
    }
}
