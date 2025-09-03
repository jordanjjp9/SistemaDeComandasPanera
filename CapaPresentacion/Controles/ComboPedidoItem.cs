using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaPresentacion.Botoneras;
using Guna.UI2.WinForms;
using Guna.UI2.WinForms.Suite;

namespace CapaPresentacion.Controles
{
    public partial class ComboPedidoItem : UserControl
    {
        // ========= Selección global (exclusiva para combos) =========
        public static ComboPedidoItem SeleccionActual { get; private set; }
        public static event EventHandler SeleccionCambio;

        // ========= Datos del combo (encabezado) =========
        public string Codigo { get; private set; } = "";
        public string Descripcion { get; private set; } = "";
        public int Cantidad { get; private set; } = 1;
        public decimal PrecioUnitario { get; private set; } = 0m;   // PU final (con extras promediados)
        public decimal Total { get { return Cantidad * PrecioUnitario; } }

        // Preferencias de agrupación
        public bool AgruparJugosIguales { get; set; } = true;  // false = un textbox por jugo
        public bool AgruparBebidasIguales { get; set; } = true; // true  = agrupar bebidas iguales

        // ========= Estado UI =========
        private readonly ToolTip _tt = new ToolTip();
        private int _baseHeight;       // alto del control sin los contenedores dinámicos
        private bool _pendingGrow = false;

        // Plantillas (opcionales) tomadas del diseñador si existen
        private Guna2TextBox _tplJugo;
        private Guna2TextBox _tplBeb;

        // ========= Modelo interno =========
        private sealed class BloqueTexto
        {
            public string Key;
            public string Descripcion;
            public decimal PrecioExtra;
            public int Cantidad;               // cuántas unidades agrupa
            public List<string> Notas = new List<string>();
            public Guna2TextBox TextBox;       // textbox visible
        }

        private readonly List<BloqueTexto> _jugos = new List<BloqueTexto>();
        private readonly List<BloqueTexto> _bebidas = new List<BloqueTexto>();
        private BloqueTexto _ultimoJugo;
        private BloqueTexto _ultimaBebida;

        public ComboPedidoItem()
        {
            InitializeComponent();

            // 1) Saca plantillas del contenedor (evita medir mal el FlowLayoutPanel)
            _tplJugo = this.Controls.Find("txtJugoDesayuno", true).OfType<Guna2TextBox>().FirstOrDefault();
            _tplBeb = this.Controls.Find("txtBebCDesay", true).OfType<Guna2TextBox>().FirstOrDefault();

            if (_tplJugo != null)
            {
                _tplJugo.Visible = false;
                if (_tplJugo.Parent is FlowLayoutPanel p1) p1.Controls.Remove(_tplJugo);
            }
            if (_tplBeb != null)
            {
                _tplBeb.Visible = false;
                if (_tplBeb.Parent is FlowLayoutPanel p2) p2.Controls.Remove(_tplBeb);
            }

            // 2) Encabezado (solo lectura)
            ConfigurarTextBoxSoloLectura(txtCombo);

            // 3) Asegura configuración de contenedores (los tuyos del diseñador)
            PrepFlow(flpJugos);
            FlowLayoutPanel flpBeb;
            if (TryFindControl("flpBebCDesayuno", out flpBeb)) PrepFlow(flpBeb);

            // 4) AutoGrow + selección
            if (txtCombo != null) txtCombo.TextChanged += (s, e) => RecalcAutoGrow();
            if (flpJugos != null)
            {
                flpJugos.SizeChanged += (s, e) => RecalcAutoGrow();
                flpJugos.ControlAdded += (s, e) => RecalcAutoGrow();
                flpJugos.ControlRemoved += (s, e) => RecalcAutoGrow();
            }
            if (flpBeb != null)
            {
                flpBeb.SizeChanged += (s, e) => RecalcAutoGrow();
                flpBeb.ControlAdded += (s, e) => RecalcAutoGrow();
                flpBeb.ControlRemoved += (s, e) => RecalcAutoGrow();
            }

            this.SizeChanged += (s, e) => RecalcAutoGrow();
            WireSelectClick(this);
            WireInnerTextBoxClicks(txtCombo);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Calcula baseHeight con layout materializado
            int hCombo = (txtCombo != null) ? txtCombo.Height : 0;
            int hJugos = (flpJugos != null) ? flpJugos.Height : 0;

            FlowLayoutPanel flpBeb;
            TryFindControl("flpBebCDesayuno", out flpBeb);
            int hBeb = (flpBeb != null) ? flpBeb.Height : 0;

            _baseHeight = this.Height - (hCombo + hJugos + hBeb);
            BeginInvoke(new Action(RecalcAutoGrow));
        }

        // ========= API ENCABEZADO =========
        public void SetCombo(string codigo, string descripcion, int cantidad, decimal precioUnitarioFinal)
        {
            Codigo = (codigo ?? string.Empty).Trim();
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? Codigo : descripcion.Trim();
            Cantidad = Math.Max(1, cantidad);
            PrecioUnitario = Math.Max(0m, precioUnitarioFinal);

            if (txtCombo != null)
            {
                txtCombo.Text = string.Format("{0} x {1} = S/ {2:0.00}",
                    Cantidad, Descripcion.ToUpperInvariant(), Total);
                _tt.SetToolTip(txtCombo, string.Format("PU: S/ {0:0.00}", PrecioUnitario));
            }

            if (IsHandleCreated) BeginInvoke(new Action(RecalcAutoGrow));
            else _pendingGrow = true;
        }

        // ========= API JUGOS =========
        public void ClearJugos()
        {
            _jugos.Clear();
            _ultimoJugo = null;
            if (flpJugos != null) flpJugos.Controls.Clear();
            RecalcAutoGrow();
        }

        /// <summary>
        /// Agrega una unidad de jugo.
        /// forzarIndividual = true ⇒ siempre crea un textbox nuevo.
        /// forzarIndividual = false ⇒ intenta agrupar jugos iguales (descripcion+precio).
        /// null ⇒ usa AgruparJugosIguales.
        /// </summary>
        public void AddJugoUnidad(string descripcion, decimal precioExtra, string notas, bool? forzarIndividual)
        {
            if (flpJugos == null) return;

            bool individual = (forzarIndividual.HasValue) ? forzarIndividual.Value : !AgruparJugosIguales;

            BloqueTexto bloque = null;
            if (!individual)
            {
                string key = KeyDe(descripcion, precioExtra);
                bloque = _jugos.FirstOrDefault(x => x.Key == key);
            }

            if (bloque == null)
            {
                bloque = new BloqueTexto();
                bloque.Key = KeyDe(descripcion, precioExtra);
                bloque.Descripcion = (descripcion ?? "").Trim();
                bloque.PrecioExtra = precioExtra;
                bloque.Cantidad = 1;
                bloque.TextBox = CrearTextBoxDesdePlantilla(_tplJugo);

                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));

                bloque.TextBox.Text = TextoDe(bloque);

                flpJugos.Controls.Add(bloque.TextBox);
                flpJugos.PerformLayout(); // fuerza layout inmediato

                _jugos.Add(bloque);
            }
            else
            {
                bloque.Cantidad += 1;
                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));
                bloque.TextBox.Text = TextoDe(bloque);
            }

            _ultimoJugo = bloque;
            RecalcAutoGrow();
        }

        public void AppendNotasAlUltimoJugo(string notas)
        {
            if (_ultimoJugo == null || string.IsNullOrWhiteSpace(notas)) return;
            _ultimoJugo.Notas.AddRange(ToNotas(notas));
            _ultimoJugo.TextBox.Text = TextoDe(_ultimoJugo);
            RecalcAutoGrow();
        }

        public decimal GetExtraPromedioJugosPorUnidad(int cantidadTotal)
        {
            if (cantidadTotal <= 0) return 0m;
            decimal extraTotal = 0m;
            foreach (var b in _jugos) extraTotal += (b.PrecioExtra * b.Cantidad);
            return extraTotal / cantidadTotal;
        }

        // ========= API BEBIDAS =========
        public void ClearBebidas()
        {
            _bebidas.Clear();
            _ultimaBebida = null;

            FlowLayoutPanel flpBeb;
            if (TryFindControl("flpBebCDesayuno", out flpBeb))
                flpBeb.Controls.Clear();

            RecalcAutoGrow();
        }

        public void AddBebidaUnidad(string descripcion, decimal precioExtra, string notas, bool? forzarIndividual)
        {
            FlowLayoutPanel flpBeb;
            if (!TryFindControl("flpBebCDesayuno", out flpBeb)) return;

            bool individual = (forzarIndividual.HasValue) ? forzarIndividual.Value : !AgruparBebidasIguales;

            BloqueTexto bloque = null;
            if (!individual)
            {
                string key = KeyDe(descripcion, precioExtra);
                bloque = _bebidas.FirstOrDefault(x => x.Key == key);
            }

            if (bloque == null)
            {
                bloque = new BloqueTexto();
                bloque.Key = KeyDe(descripcion, precioExtra);
                bloque.Descripcion = (descripcion ?? "").Trim();
                bloque.PrecioExtra = precioExtra;
                bloque.Cantidad = 1;
                bloque.TextBox = CrearTextBoxDesdePlantilla(_tplBeb);

                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));

                bloque.TextBox.Text = TextoDe(bloque);

                flpBeb.Controls.Add(bloque.TextBox);
                flpBeb.PerformLayout();

                _bebidas.Add(bloque);
            }
            else
            {
                bloque.Cantidad += 1;
                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));
                bloque.TextBox.Text = TextoDe(bloque);
            }

            _ultimaBebida = bloque;
            RecalcAutoGrow();
        }

        public void AppendNotasALaUltimaBebida(string notas)
        {
            if (_ultimaBebida == null || string.IsNullOrWhiteSpace(notas)) return;
            _ultimaBebida.Notas.AddRange(ToNotas(notas));
            _ultimaBebida.TextBox.Text = TextoDe(_ultimaBebida);
            RecalcAutoGrow();
        }

        public decimal GetExtraPromedioBebidasPorUnidad(int cantidadTotal)
        {
            if (cantidadTotal <= 0) return 0m;
            decimal extraTotal = 0m;
            foreach (var b in _bebidas) extraTotal += (b.PrecioExtra * b.Cantidad);
            return extraTotal / cantidadTotal;
        }

        public decimal GetExtraPromedioTotalPorUnidad(int cantidadTotal)
        {
            return GetExtraPromedioJugosPorUnidad(cantidadTotal) +
                   GetExtraPromedioBebidasPorUnidad(cantidadTotal);
        }

        // ========= Selección =========
        private void WireSelectClick(Control root)
        {
            if (root == null) return;

            root.Click -= Any_Click_Select; root.Click += Any_Click_Select;
            root.MouseDown -= Any_Click_Select; root.MouseDown += Any_Click_Select;

            foreach (Control c in root.Controls)
                WireSelectClick(c);
        }

        private void WireInnerTextBoxClicks(Control guna2TextBox)
        {
            TextBox inner = TryGetInnerTextBox(guna2TextBox);
            if (inner == null) return;

            inner.ReadOnly = true;
            inner.ShortcutsEnabled = false;
            inner.Cursor = Cursors.Hand;

            inner.Click -= Any_Click_Select; inner.Click += Any_Click_Select;
            inner.MouseDown -= Any_Click_Select; inner.MouseDown += Any_Click_Select;
        }

        private void Any_Click_Select(object sender, EventArgs e) { SeleccionarEste(); }

        public void SeleccionarEste()
        {
            if (SeleccionActual == this) return;

            ComboPedidoItem anterior = SeleccionActual;
            SeleccionActual = this;

            if (anterior != null) anterior.SetVisualSelected(false);
            this.SetVisualSelected(true);

            ScrollableControl scrollable = this.Parent as ScrollableControl;
            if (scrollable != null) scrollable.ScrollControlIntoView(this);

            EventHandler handler = SeleccionCambio;
            if (handler != null) handler.Invoke(null, EventArgs.Empty);
        }

        public void SetVisualSelected(bool selected)
        {
            this.BorderStyle = selected ? BorderStyle.FixedSingle : BorderStyle.None;
        }

        public static void Seleccionar(ComboPedidoItem item, bool scrollIntoView)
        {
            if (SeleccionActual == item) return;

            if (SeleccionActual != null) SeleccionActual.SetVisualSelected(false);
            SeleccionActual = item;

            if (SeleccionActual != null)
            {
                SeleccionActual.SetVisualSelected(true);

                if (scrollIntoView)
                {
                    ScrollableControl sc = GetScrollableAncestor(SeleccionActual);
                    if (sc != null) sc.ScrollControlIntoView(SeleccionActual);
                }
            }

            EventHandler handler = SeleccionCambio;
            if (handler != null) handler.Invoke(null, EventArgs.Empty);
        }

        private static ScrollableControl GetScrollableAncestor(Control c)
        {
            for (Control p = c != null ? c.Parent : null; p != null; p = p.Parent)
                if (p is ScrollableControl) return (ScrollableControl)p;
            return null;
        }

        // ========= AutoGrow / medidas =========
        private void RecalcAutoGrow()
        {
            if (!IsHandleCreated) return;

            AjustarAnchosContenedor(flpJugos);
            FlowLayoutPanel flpBeb;
            if (TryFindControl("flpBebCDesayuno", out flpBeb))
                AjustarAnchosContenedor(flpBeb);

            int hCombo = AltoNecesario(txtCombo);
            int hJugos = (flpJugos != null) ? flpJugos.Height : 0;
            int hBeb = 0;
            FlowLayoutPanel flpBeb2;
            if (TryFindControl("flpBebCDesayuno", out flpBeb2))
                hBeb = flpBeb2.Height;

            SuspendLayout();

            if (txtCombo != null && txtCombo.Height != hCombo)
                txtCombo.Height = hCombo;

            this.Height = _baseHeight + hCombo + hJugos + hBeb;

            ResumeLayout();
        }

        private void AjustarAnchosContenedor(FlowLayoutPanel cont)
        {
            if (cont == null) return;

            int ancho = Math.Max(1, cont.ClientSize.Width);

            foreach (Control c in cont.Controls)
            {
                if (c.Width != ancho)
                {
                    c.Width = ancho;
                    AjustarAlturaControl(c);
                }
            }
        }

        private void AjustarAlturaControl(Control c)
        {
            Guna2TextBox tb = c as Guna2TextBox;
            if (tb == null) return;

            int w = Math.Max(1, tb.ClientSize.Width);
            using (Graphics g = tb.CreateGraphics())
            {
                StringFormat sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.MeasureTrailingSpaces);
                SizeF size = g.MeasureString((tb.Text ?? "") + "\nA", tb.Font, w, sf);
                int needed = (int)Math.Ceiling(size.Height) + 4;
                if (needed < 28) needed = 28;
                if (tb.Height != needed) tb.Height = needed;
            }
        }

        private int AltoNecesario(Control tb)
        {
            if (tb == null) return 0;

            string t = Convert.ToString(tb.GetType().GetProperty("Text").GetValue(tb, null)) ?? string.Empty;
            int w = tb.ClientSize.Width > 0 ? tb.ClientSize.Width : Math.Max(1, tb.Width - 6);

            if (t.Length == 0) return Math.Max(24, tb.Font.Height + 6);

            TextBox inner = TryGetInnerTextBox(tb);
            if (inner != null && inner.IsHandleCreated)
            {
                int last = Math.Max(0, t.Length - 1);
                while (last >= 0 && (t[last] == '\r' || t[last] == '\n')) last--;
                if (last < 0) last = 0;

                try { inner.WordWrap = true; } catch { }

                Point pt = inner.GetPositionFromCharIndex(last);
                int needed = pt.Y + inner.Font.Height + 6;
                return Math.Max(28, needed);
            }

            using (Graphics g = tb.CreateGraphics())
            {
                StringFormat sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.MeasureTrailingSpaces);
                SizeF size = g.MeasureString(t + "\nA", tb.Font, w, sf);
                int needed = (int)Math.Ceiling(size.Height) + 4;
                return Math.Max(28, needed);
            }
        }

        // ========= Helpers =========
        private static void ConfigurarTextBoxSoloLectura(Guna2TextBox tb)
        {
            if (tb == null) return;

            tb.Multiline = true;
            tb.WordWrap = true;
            tb.AcceptsReturn = true;
            tb.ScrollBars = ScrollBars.None;
            tb.ReadOnly = true;
            tb.Enabled = true;
            tb.Cursor = Cursors.Hand;
            tb.Dock = DockStyle.Top;
        }

        private Guna2TextBox CrearTextBoxDesdePlantilla(Guna2TextBox plantilla)
        {
            var tb = new Guna2TextBox();

            if (plantilla != null)
            {
                tb.Font = plantilla.Font;
                tb.ForeColor = plantilla.ForeColor;
                tb.FillColor = plantilla.FillColor;
                tb.BorderRadius = plantilla.BorderRadius;
                tb.BorderColor = plantilla.BorderColor;
                tb.BorderThickness = plantilla.BorderThickness;
                tb.Padding = plantilla.Padding;
                tb.Margin = new Padding(0, 4, 0, 0);
                tb.PlaceholderText = plantilla.PlaceholderText;
                tb.TextAlign = plantilla.TextAlign;
            }
            else
            {
                tb.BorderRadius = 6;
                tb.BorderThickness = 1;
                tb.Margin = new Padding(0, 4, 0, 0);
            }

            tb.Dock = DockStyle.Top;
            tb.Width = (flpJugos != null ? flpJugos.ClientSize.Width
                     : this.ClientSize.Width > 0 ? this.ClientSize.Width : 300);
            tb.MinimumSize = new Size(tb.Width, 28);

            tb.Multiline = true;
            tb.WordWrap = true;
            tb.AcceptsReturn = true;
            tb.ScrollBars = ScrollBars.None;
            tb.ReadOnly = true;              // si quieres editable, cambia a false
            tb.Enabled = true;
            tb.Cursor = Cursors.Hand;
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            tb.TextChanged += (s, e) => RecalcAutoGrow();
            tb.Click += (s, e) => SeleccionarEste();

            return tb;
        }

        private static string KeyDe(string descripcion, decimal precio)
        {
            string d = (descripcion ?? "").Trim().ToUpperInvariant();
            return d + "|" + precio.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static IEnumerable<string> ToNotas(string notas)
        {
            string t = (notas ?? "").Replace("\r\n", "\n");
            foreach (string raw in t.Split('\n'))
            {
                string s = raw.Trim();
                if (s.Length == 0) continue;
                if (s.StartsWith("-")) s = s.TrimStart('-').Trim();
                yield return s;
            }
        }

        private static string TextoDe(BloqueTexto b)
        {
            var sb = new StringBuilder();

            if (b.Cantidad <= 1)
                sb.Append("1 x ").Append(b.Descripcion);
            else
                sb.Append(b.Cantidad).Append(" x ").Append(b.Descripcion);

            if (b.PrecioExtra > 0)
                sb.Append(" = S/ ").Append(b.PrecioExtra.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            foreach (string n in b.Notas)
                sb.AppendLine().Append("  - ").Append(n);

            return sb.ToString();
        }

        private static TextBox TryGetInnerTextBox(Control guna2TextBox)
        {
            if (guna2TextBox == null) return null;
            try
            {
                PropertyInfo prop = guna2TextBox.GetType().GetProperty(
                    "TextBox", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop != null ? prop.GetValue(guna2TextBox, null) as TextBox : null;
            }
            catch
            {
                return null;
            }
        }

        private static void PrepFlow(FlowLayoutPanel flp)
        {
            if (flp == null) return;
            flp.FlowDirection = FlowDirection.TopDown;
            flp.WrapContents = false;
            flp.AutoSize = true;
            flp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flp.Dock = DockStyle.Top;
            flp.Margin = new Padding(0);
            flp.Padding = new Padding(0);
        }

        private bool TryFindControl<T>(string name, out T ctrl) where T : Control
        {
            ctrl = this.Controls.Find(name, true).OfType<T>().FirstOrDefault();
            return (ctrl != null);
        }
    }
}