using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using CapaPresentacion.Helpers;

namespace CapaPresentacion.Controles
{
    public partial class MenuPedidoItem : UserControl, ILineaSeleccionable
    {
        public string Codigo { get; private set; } = "";
        public string Descripcion { get; private set; } = "";
        public int Cantidad { get; private set; } = 1;
        public decimal PrecioUnitario { get; private set; } = 0m;
        public decimal Total => Cantidad * PrecioUnitario;

        private Guna2TextBox _txtMenu;
        private Guna2TextBox _txtChich;

        private int _baseHeight;
        private bool _pendingGrow;
        public enum ZonaNotas { Ninguna = 0, Menu = 1, Chicha = 2 }
        public ZonaNotas ZonaActiva { get; private set; } = ZonaNotas.Menu;

        // ==== selección global ====
        public Control View => this;
        public void SetVisualSelected(bool sel) => BorderStyle = sel ? BorderStyle.FixedSingle : BorderStyle.None;

        public MenuPedidoItem()
        {
            InitializeComponent();

            _txtMenu = this.Controls.Find("txtMenu", true).OfType<Guna2TextBox>().FirstOrDefault();
            _txtChich = this.Controls.Find("txtChich", true).OfType<Guna2TextBox>().FirstOrDefault();

            Prep(_txtMenu);
            Prep(_txtChich);

            //if (_txtMenu != null) _txtMenu.TextChanged += (_, __) => Recalc();
            //if (_txtChich != null) _txtChich.TextChanged += (_, __) => Recalc();
            if (_txtMenu != null)
            {
                _txtMenu.Click += (_, __) => { ZonaActiva = ZonaNotas.Menu; LineaSelection.Select(this, true); };
                _txtMenu.MouseDown += (_, __) => { ZonaActiva = ZonaNotas.Menu; LineaSelection.Select(this, true); };
            }
            if (_txtChich != null)
            {
                _txtChich.Click += (_, __) => { ZonaActiva = ZonaNotas.Chicha; LineaSelection.Select(this, true); };
                _txtChich.MouseDown += (_, __) => { ZonaActiva = ZonaNotas.Chicha; LineaSelection.Select(this, true); };
            }

            this.SizeChanged += (_, __) => Recalc();

            // seleccionar con click en cualquier parte
            WireSelectClick(this);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int hMenu = _txtMenu != null ? _txtMenu.Height : 0;
            int hChich = _txtChich != null ? _txtChich.Height : 0;
            _baseHeight = this.Height - (hMenu + hChich);
            Recalc();
        }
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_pendingGrow) { _pendingGrow = false; Recalc(); }
        }

        private static void Prep(Guna2TextBox tb)
        {
            if (tb == null) return;
            tb.Multiline = true;
            tb.ReadOnly = true;
            tb.WordWrap = true;
            tb.ScrollBars = ScrollBars.None;
            tb.Dock = DockStyle.Top;
            tb.Cursor = Cursors.Hand;
        }
        private void WireSelectClick(Control root)
        {
            root.Click -= (_, __) => LineaSelection.Select(this, true);
            root.Click += (_, __) => LineaSelection.Select(this, true);
            root.MouseDown -= (_, __) => LineaSelection.Select(this, true);
            root.MouseDown += (_, __) => LineaSelection.Select(this, true);
            foreach (Control c in root.Controls) WireSelectClick(c);
        }
        // ===== API =====
        public void SetMenu(string codigo, string descripcion, int cantidad, decimal pu)
        {
            Codigo = (codigo ?? "").Trim();
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? Codigo : descripcion.Trim();
            Cantidad = Math.Max(1, cantidad);
            PrecioUnitario = Math.Max(0m, pu);

            if (_txtMenu != null)
                _txtMenu.Text = $"{Cantidad} x {Descripcion.ToUpperInvariant()} = S/ {Total:0.00}";

            Recalc();
        }
        // En CapaPresentacion.Controles.MenuPedidoItem
        public void SetChicha(string descripcion, string notas, int cantidad = 1)
        {
            if (_txtChich == null) return;

            var sb = new StringBuilder();
            int cant = Math.Max(1, cantidad);

            sb.Append(cant).Append(" x ").Append((descripcion ?? "").Trim().ToUpperInvariant());

            string norm = NormalizeNotes(notas);
            if (!string.IsNullOrEmpty(norm))
                sb.AppendLine().Append(norm);

            _txtChich.Text = sb.ToString();
            Recalc();
        }
        public void AppendNotasChicha(string notas)
        {
            if (_txtChich == null) return;

            var text = _txtChich.Text ?? "";
            var extra = NormalizeNotes(notas);
            if (string.IsNullOrEmpty(extra)) return;

            if (text.Length > 0 && !text.EndsWith(Environment.NewLine))
                text += Environment.NewLine;

            _txtChich.Text = text + extra;
            Recalc();
        }
        private static string NormalizeNotes(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            var lines = raw.Replace("\r\n", "\n").Replace("\r", "\n")
                           .Split('\n')
                           .Select(l => (l ?? "").Trim())
                           .Where(l => l.Length > 0)
                           .Select(l => l.StartsWith("-") ? l : "- " + l);
            return string.Join(Environment.NewLine, lines);
        }

        // ===== autogrow =====
        private void Recalc()
        {
            if (!IsHandleCreated) { _pendingGrow = true; return; }

            int h1 = AltoNecesario(_txtMenu);
            int h2 = AltoNecesario(_txtChich);

            SuspendLayout();
            if (_txtMenu != null && _txtMenu.Height != h1) _txtMenu.Height = h1;
            if (_txtChich != null && _txtChich.Height != h2) _txtChich.Height = h2;
            this.Height = _baseHeight + h1 + h2;
            ResumeLayout();
        }

        private static int AltoNecesario(Guna2TextBox tb)
        {
            if (tb == null) return 0;
            string t = tb.Text ?? string.Empty;
            if (t.Length == 0) return Math.Max(28, tb.Font.Height + 8);

            var inner = TryInner(tb);
            if (inner != null && inner.IsHandleCreated)
            {
                try { inner.WordWrap = true; inner.ScrollBars = ScrollBars.None; } catch { }
                int last = Math.Max(0, t.Length - 1);
                while (last >= 0 && (t[last] == '\r' || t[last] == '\n')) last--;
                if (last < 0) last = 0;
                var pt = inner.GetPositionFromCharIndex(last);
                return Math.Max(28, pt.Y + inner.Font.Height + 14);
            }

            using (var g = tb.CreateGraphics())
            {
                var sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.MeasureTrailingSpaces);
                var size = g.MeasureString(t + "\nA", tb.Font, Math.Max(1, tb.ClientSize.Width), sf);
                return Math.Max(28, (int)Math.Ceiling(size.Height) + 10);
            }
        }

        private static TextBox TryInner(Control guna2TextBox)
        {
            try
            {
                var prop = guna2TextBox.GetType().GetProperty("TextBox",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(guna2TextBox, null) as TextBox;
            }
            catch { return null; }
        }
        // --- NOTAS MENU ---
        public string GetNotasMenuRaw()
        {
            if (_txtMenu == null) return string.Empty;
            var t = _txtMenu.Text ?? string.Empty;
            t = t.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = t.Split('\n');
            return (lines.Length <= 1) ? string.Empty
                                       : string.Join(Environment.NewLine, lines.Skip(1));
        }
        public void SetNotasMenu(string notas)
        {
            if (_txtMenu == null) return;
            var header = ObtenerPrimeraLinea(_txtMenu.Text);
            var norm = NormalizeNotes(notas);
            _txtMenu.Text = string.IsNullOrEmpty(norm) ? header : header + Environment.NewLine + norm;
            Recalc();
        }

        // --- NOTAS CHICHA ---
        public string GetNotasChichaRaw()
        {
            if (_txtChich == null) return string.Empty;
            var t = _txtChich.Text ?? string.Empty;
            t = t.Replace("\r\n", "\n").Replace("\r", "\n");
            var lines = t.Split('\n');
            return (lines.Length <= 1) ? string.Empty
                                       : string.Join(Environment.NewLine, lines.Skip(1));
        }
        public void SetNotasChicha(string notas)
        {
            if (_txtChich == null) return;
            var header = ObtenerPrimeraLinea(_txtChich.Text);
            var norm = NormalizeNotes(notas);
            _txtChich.Text = string.IsNullOrEmpty(norm) ? header : header + Environment.NewLine + norm;
            Recalc();
        }

        // helper para tomar la primera línea de un textbox multilínea
        private static string ObtenerPrimeraLinea(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            var t = text.Replace("\r\n", "\n").Replace("\r", "\n");
            int nl = t.IndexOf('\n');
            return (nl < 0) ? t : t.Substring(0, nl);
        }

        // API genérica según zona activa (útil desde el formulario)
        public string GetNotasRaw(ZonaNotas zona)
            => zona == ZonaNotas.Chicha ? GetNotasChichaRaw()
               : zona == ZonaNotas.Menu ? GetNotasMenuRaw()
               : string.Empty;

        public void SetNotas(ZonaNotas zona, string notas)
        {
            if (zona == ZonaNotas.Chicha) SetNotasChicha(notas);
            else if (zona == ZonaNotas.Menu) SetNotasMenu(notas);
        }

    }
}
