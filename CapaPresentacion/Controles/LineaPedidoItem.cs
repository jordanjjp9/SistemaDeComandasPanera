using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using CapaPresentacion.Helpers;   // <-- para ILineaSeleccionable y LineaSelection

namespace CapaPresentacion.Controles
{
    public partial class LineaPedidoItem : UserControl, ILineaSeleccionable
    {
        // ===== Datos de la línea =====
        public string Codigo { get; private set; } = string.Empty;
        public string Descripcion { get; private set; } = string.Empty;
        public int Cantidad { get; private set; } = 1;
        public decimal PrecioUnitario { get; private set; } = 0m;
        public decimal Importe { get { return Cantidad * PrecioUnitario; } }

        private readonly List<string> _notas = new List<string>();
        private readonly ToolTip _tt = new ToolTip();
        private bool _pendingGrow = false;

        public string Notas { get; private set; } = string.Empty;

        // ===== ILineaSeleccionable =====
        public Control View { get { return this; } }
        public void SetVisualSelected(bool sel) { BorderStyle = sel ? BorderStyle.FixedSingle : BorderStyle.None; }

        public LineaPedidoItem()
        {
            InitializeComponent();

            // Un único textbox (encabezado + notas)
            txtProducto.Multiline = true;
            txtProducto.WordWrap = true;
            txtProducto.AcceptsReturn = true;
            txtProducto.ScrollBars = ScrollBars.None;
            txtProducto.ReadOnly = true;     // edición por diálogo
            txtProducto.Enabled = true;      // importante para medir
            txtProducto.Cursor = Cursors.Hand;
            txtProducto.Dock = DockStyle.Top;
            txtProducto.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            Load += (s, e) => RequestAutoGrow();
            txtProducto.TextChanged += (s, e) => RequestAutoGrow();
            SizeChanged += (s, e) => RequestAutoGrow();

            // selección con click
            WireSelectClick(this);
            var inner = TryGetInnerTextBox(txtProducto);
            if (inner != null)
            {
                inner.Click -= Any_Click_Select; inner.Click += Any_Click_Select;
                inner.MouseDown -= Any_Click_Select; inner.MouseDown += Any_Click_Select;
            }
        }

        // ===== API =====

        public void Configurar(string codigo, string descripcion, int cantidad, decimal precioUnitario, string notasIniciales)
        {
            Codigo = (codigo ?? string.Empty).Trim();
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? Codigo : descripcion.Trim();
            Cantidad = Math.Max(1, cantidad);
            PrecioUnitario = Math.Max(0m, precioUnitario);

            SetNotas(notasIniciales);
            PintarTexto();

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

        private static string NormalizarNotas(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var lines = (raw ?? string.Empty)
                .Replace("\r\n", "\n").Replace("\r", "\n")
                .Split('\n')
                .Select(l => (l ?? string.Empty).Trim())
                .Where(l => l.Length > 0)
                .Select(l => l.StartsWith("-") ? l : "- " + l);
            return string.Join(Environment.NewLine, lines);
        }

        private void RedibujarTextoConNotas()
        {
            string header = string.Format("{0} x {1} = S/ {2:0.00}", Cantidad, Descripcion.ToUpperInvariant(), Importe);

            if (txtProducto != null)
                txtProducto.Text = string.IsNullOrEmpty(Notas) ? header : header + Environment.NewLine + Notas;

            try { BeginInvoke((Action)(() => RecalcAutoGrow())); } catch { }
        }

        public string GetNotasRaw() { return string.Join(Environment.NewLine, _notas); }

        // ===== selección local -> selector global =====
        private void WireSelectClick(Control root)
        {
            root.Click -= Any_Click_Select; root.Click += Any_Click_Select;
            root.MouseDown -= Any_Click_Select; root.MouseDown += Any_Click_Select;
            foreach (Control c in root.Controls) WireSelectClick(c);
        }

        private void Any_Click_Select(object s, EventArgs e)
        {
            LineaSelection.Select(this, true);   // << selección global única
        }

        // ===== texto =====
        private void PintarTexto()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(Cantidad).Append(" x ").Append(Descripcion.ToUpperInvariant())
              .Append(" = S/ ").Append(Importe.ToString("0.00",
                  System.Globalization.CultureInfo.InvariantCulture));

            foreach (var n in _notas)
                sb.AppendLine().Append("  - ").Append(n);

            txtProducto.Text = sb.ToString();
        }

        private static IEnumerable<string> ParseLines(string raw)
        {
            var norm = (raw ?? string.Empty).Replace("\r\n", "\n");
            foreach (var line in norm.Split('\n'))
            {
                var s = (line ?? string.Empty).Trim();
                if (s.Length == 0) continue;
                if (s.StartsWith("-")) s = s.TrimStart('-').Trim();
                yield return s;
            }
        }

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
            if (txtProducto.Height != hTxt) txtProducto.Height = hTxt;

            int nuevoAlto = txtProducto.Bottom + this.Padding.Bottom;
            if (Height != nuevoAlto) Height = nuevoAlto;
            ResumeLayout();
        }

        private static int AltoNecesario(Guna2TextBox tb)
        {
            if (tb == null) return 0;
            string t = tb.Text ?? string.Empty;
            if (t.Length == 0) return Math.Max(28, tb.Font.Height + 8);

            var inner = TryGetInnerTextBox(tb);
            if (inner != null && inner.IsHandleCreated)
            {
                try { inner.WordWrap = true; inner.ScrollBars = ScrollBars.None; } catch { }
                int last = t.Length - 1;
                while (last >= 0 && (t[last] == '\r' || t[last] == '\n')) last--;
                if (last < 0) last = 0;

                var pt = inner.GetPositionFromCharIndex(last);
                int needed = pt.Y + inner.Font.Height + 14;
                return Math.Max(28, needed);
            }

            using (var g = tb.CreateGraphics())
            {
                var sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.MeasureTrailingSpaces);
                var size = g.MeasureString(t + "\nA", tb.Font, Math.Max(1, tb.ClientSize.Width), sf);
                int needed = (int)Math.Ceiling(size.Height) + 10;
                return Math.Max(28, needed);
            }
        }

        private static TextBox TryGetInnerTextBox(Control guna2TextBox)
        {
            try
            {
                var prop = guna2TextBox.GetType().GetProperty(
                    "TextBox", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop != null ? prop.GetValue(guna2TextBox, null) as TextBox : null;
            }
            catch { return null; }
        }
    }
}
