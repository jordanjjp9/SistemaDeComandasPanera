using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using CapaPresentacion.Helpers;
using CapaEntidad;

namespace CapaPresentacion.Controles
{
    public partial class MenuPedidoItem : UserControl, ILineaSeleccionable
    {
        private DetalleRef _refDetalle;

        public void SetRefDetalle(DetalleRef r) => _refDetalle = r;
        public DetalleRef GetRefDetalle() => _refDetalle;
        public bool TieneRefDetalle => _refDetalle != null;

        // ===== Datos del MENÚ (encabezado) =====
        public string Codigo { get; private set; } = "";
        public string Descripcion { get; private set; } = "";
        public int Cantidad { get; private set; } = 1;
        public decimal PrecioUnitario { get; private set; } = 0m;
        public decimal Total { get { return Cantidad * PrecioUnitario; } }

        // ===== Datos de la CHICHA (solo para export) =====
        private string _codigoChicha = "";
        private string _descripcionChicha = "";
        private int _cantChicha = 0; // 0 => no hay chicha

        // ===== Notas separadas =====
        private readonly List<string> _notasMenu = new List<string>();
        private readonly List<string> _notasChicha = new List<string>();

        // ===== Export plano =====
        public sealed class LineaExport
        {
            public string Codigo { get; set; }  // CDG_PROD (10 dígitos lo resuelve el DAO)
            public string Descripcion { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitarioConIgv { get; set; } // PU mostrado en UI (CON IGV)
            public string Notas { get; set; }     // OBS_PPRD
        }

        public IEnumerable<LineaExport> GetLineasExport()
        {
            // 1) Menú (siempre)
            yield return new LineaExport
            {
                Codigo = this.Codigo ?? "",
                Descripcion = this.Descripcion ?? "",
                Cantidad = this.Cantidad,
                PrecioUnitarioConIgv = this.PrecioUnitario,
                Notas = FormatearNotas(_notasMenu)
            };

            // 2) Chicha (si hay)
            if (_cantChicha > 0)
            {
                yield return new LineaExport
                {
                    Codigo = _codigoChicha ?? "",
                    Descripcion = _descripcionChicha ?? "",
                    Cantidad = _cantChicha,
                    PrecioUnitarioConIgv = 0m, // chicha sin precio
                    Notas = FormatearNotas(_notasChicha)
                };
            }
        }

        // ===== UI =====
        private Guna2TextBox _txtMenu;
        private Guna2TextBox _txtChich;

        private int _baseHeight;
        private bool _pendingGrow;

        public enum ZonaNotas { Ninguna = 0, Menu = 1, Chicha = 2 }
        public ZonaNotas ZonaActiva { get; private set; } = ZonaNotas.Menu;

        // ===== ILineaSeleccionable =====
        public Control View { get { return this; } }
        public void SetVisualSelected(bool sel) { this.BorderStyle = sel ? BorderStyle.FixedSingle : BorderStyle.None; }

        private void Any_Click_Select(object s, EventArgs e) { LineaSelection.Select(this, true); }

        public MenuPedidoItem()
        {
            InitializeComponent();

            _txtMenu = this.Controls.Find("txtMenu", true).OfType<Guna2TextBox>().FirstOrDefault();
            _txtChich = this.Controls.Find("txtChich", true).OfType<Guna2TextBox>().FirstOrDefault();

            PrepTextBox(_txtMenu);
            PrepTextBox(_txtChich);

            // Clicks sobre el propio Guna2TextBox (por si cae en bordes)
            if (_txtMenu != null)
            {
                _txtMenu.Click += (s, e) => { ZonaActiva = ZonaNotas.Menu; LineaSelection.Select(this, true); };
                _txtMenu.MouseDown += (s, e) => { ZonaActiva = ZonaNotas.Menu; LineaSelection.Select(this, true); };
                _txtMenu.DoubleClick += (s, e) => { try { System.Media.SystemSounds.Beep.Play(); } catch { } };
            }
            if (_txtChich != null)
            {
                _txtChich.Click += (s, e) => { ZonaActiva = ZonaNotas.Chicha; LineaSelection.Select(this, true); };
                _txtChich.MouseDown += (s, e) => { ZonaActiva = ZonaNotas.Chicha; LineaSelection.Select(this, true); };
                _txtChich.DoubleClick += (s, e) => { try { System.Media.SystemSounds.Beep.Play(); } catch { } };
            }

            // 👇 NUEVO: cablear también el TextBox interno de Guna (donde realmente cae el click)
            WireInner(_txtMenu, ZonaNotas.Menu);
            WireInner(_txtChich, ZonaNotas.Chicha);

            this.SizeChanged += (s, e) => Recalc();

            // selección con click en cualquier parte del control
            WireSelectClick(this);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            int h1 = (_txtMenu != null) ? _txtMenu.Height : 0;
            int h2 = (_txtChich != null) ? _txtChich.Height : 0;
            _baseHeight = this.Height - (h1 + h2);
            Recalc();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_pendingGrow) { _pendingGrow = false; Recalc(); }
        }

        // ===== API =====
        public void SetMenu(string codigo, string descripcion, int cantidad, decimal pu)
        {
            this.Codigo = (codigo ?? "").Trim();
            this.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? this.Codigo : descripcion.Trim();
            this.Cantidad = (cantidad > 0) ? cantidad : 1;
            this.PrecioUnitario = (pu >= 0m) ? pu : 0m;

            // no toco _notasMenu aquí; se modifican con SetNotas/Append
            PintarMenu();
            Recalc();
        }

        // Compatibilidad: firma antigua sin código de chicha
        public void SetChicha(string descripcion, string notas, int cantidad)
        {
            SetChicha(string.Empty, descripcion, notas, cantidad);
        }

        // Nueva firma con código (recomendado para exportar CDG_PROD real de la chicha)
        public void SetChicha(string codigo, string descripcion, string notas, int cantidad)
        {
            _codigoChicha = (codigo ?? "").Trim();
            _descripcionChicha = string.IsNullOrWhiteSpace(descripcion) ? _codigoChicha : descripcion.Trim();
            _cantChicha = (cantidad > 0) ? cantidad : 1;

            _notasChicha.Clear();
            if (!string.IsNullOrWhiteSpace(notas))
                _notasChicha.AddRange(ParseNotas(notas));

            PintarChicha();
            Recalc();
        }

        public void AppendNotasChicha(string notas)
        {
            if (string.IsNullOrWhiteSpace(notas)) return;
            _notasChicha.AddRange(ParseNotas(notas));
            PintarChicha();
            Recalc();
        }

        public void AppendNotasMenu(string notas)
        {
            if (string.IsNullOrWhiteSpace(notas)) return;
            _notasMenu.AddRange(ParseNotas(notas));
            PintarMenu();
            Recalc();
        }

        // === Lectura/edición de notas por zona ===
        public string GetNotasRaw(ZonaNotas zona)
        {
            if (zona == ZonaNotas.Menu) return FormatearNotas(_notasMenu);
            if (zona == ZonaNotas.Chicha) return FormatearNotas(_notasChicha);
            return string.Empty;
        }

        public void SetNotas(ZonaNotas zona, string notas)
        {
            var parsed = ParseNotas(notas);

            if (zona == ZonaNotas.Menu)
            {
                _notasMenu.Clear();
                _notasMenu.AddRange(parsed);
                PintarMenu();
            }
            else if (zona == ZonaNotas.Chicha)
            {
                _notasChicha.Clear();
                _notasChicha.AddRange(parsed);
                PintarChicha();
            }
            Recalc();
        }

        // ===== Pintado =====
        private void PintarMenu()
        {
            if (_txtMenu == null) return;

            var sb = new StringBuilder();
            sb.AppendFormat("{0} x {1} = S/ {2:0.00}", Cantidad, (Descripcion ?? "").ToUpperInvariant(), Total);

            foreach (var n in _notasMenu)
                sb.AppendLine().Append("  - ").Append(n);

            _txtMenu.Text = sb.ToString();
        }

        private void PintarChicha()
        {
            if (_txtChich == null) return;

            if (_cantChicha <= 0)
            {
                _txtChich.Text = string.Empty;
                return;
            }

            var sb = new StringBuilder();
            sb.Append(_cantChicha).Append(" x ").Append((_descripcionChicha ?? "").ToUpperInvariant());

            if (_notasChicha.Count > 0)
            {
                foreach (var n in _notasChicha)
                    sb.AppendLine().Append("  - ").Append(n);
            }

            _txtChich.Text = sb.ToString();
        }

        // ===== Autogrow =====
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

            TextBox inner = TryInner(tb);
            if (inner != null && inner.IsHandleCreated)
            {
                try { inner.WordWrap = true; inner.ScrollBars = ScrollBars.None; } catch { }
                int last = Math.Max(0, t.Length - 1);
                while (last >= 0 && (t[last] == '\r' || t[last] == '\n')) last--;
                if (last < 0) last = 0;
                Point pt = inner.GetPositionFromCharIndex(last);
                return Math.Max(28, pt.Y + inner.Font.Height + 14);
            }

            using (var g = tb.CreateGraphics())
            {
                var sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.MeasureTrailingSpaces);
                SizeF size = g.MeasureString(t + "\nA", tb.Font, Math.Max(1, tb.ClientSize.Width), sf);
                return Math.Max(28, (int)Math.Ceiling(size.Height) + 10);
            }
        }

        // ===== Helpers =====
        private static void PrepTextBox(Guna2TextBox tb)
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
            root.Click -= Any_Click_Select; root.Click += Any_Click_Select;
            root.MouseDown -= Any_Click_Select; root.MouseDown += Any_Click_Select;
            foreach (Control c in root.Controls) WireSelectClick(c);
        }

        private static TextBox TryInner(Control guna2TextBox)
        {
            try
            {
                var prop = guna2TextBox.GetType().GetProperty("TextBox",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop != null ? prop.GetValue(guna2TextBox, null) as TextBox : null;
            }
            catch { return null; }
        }

        // 👇 NUEVO: cableado del TextBox interno para asegurar selección
        private void WireInner(Guna2TextBox tb, ZonaNotas zona)
        {
            if (tb == null) return;
            var inner = TryInner(tb);
            if (inner == null) return;

            inner.ReadOnly = true;
            inner.ShortcutsEnabled = false;
            inner.Cursor = Cursors.Hand;

            // limpio handlers previos por seguridad
            inner.Click -= Any_Click_Select;
            inner.MouseDown -= Any_Click_Select;
            inner.DoubleClick -= Inner_DoubleClick_NoEdit;

            inner.Click += (s, e) => { ZonaActiva = zona; LineaSelection.Select(this, true); };
            inner.MouseDown += (s, e) => { ZonaActiva = zona; LineaSelection.Select(this, true); };
            inner.DoubleClick += Inner_DoubleClick_NoEdit;
        }

        private void Inner_DoubleClick_NoEdit(object sender, EventArgs e)
        {
            try { System.Media.SystemSounds.Beep.Play(); } catch { }
            // sólo marcamos selección, NO abrir editores aquí
        }

        private static List<string> ParseNotas(string raw)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return list;

            string t = raw.Replace("\r\n", "\n").Replace("\r", "\n");
            foreach (var r in t.Split('\n'))
            {
                var s = (r ?? "").Trim();
                if (s.Length == 0) continue;
                if (s.StartsWith("-")) s = s.TrimStart('-').Trim();
                list.Add(s);
            }
            return list;
        }

        private static string FormatearNotas(IEnumerable<string> notas)
        {
            var arr = (notas ?? new List<string>())
                      .Select(n => n == null ? "" : n.Trim())
                      .Where(n => n.Length > 0)
                      .Select(n => n.StartsWith("-") ? n : "- " + n);
            return string.Join(Environment.NewLine, arr);
        }
    }
}
