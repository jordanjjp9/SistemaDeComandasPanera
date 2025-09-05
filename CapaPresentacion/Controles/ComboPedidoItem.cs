using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using CapaPresentacion.Helpers;   // <-- para ILineaSeleccionable y LineaSelection

namespace CapaPresentacion.Controles
{
    public partial class ComboPedidoItem : UserControl, ILineaSeleccionable
    {
        // ===== Encabezado del combo =====
        public string Codigo { get; private set; } = "";
        public string Descripcion { get; private set; } = "";
        public int Cantidad { get; private set; } = 1;
        public decimal PrecioUnitario { get; private set; } = 0m;   // PU final
        public decimal Total { get { return Cantidad * PrecioUnitario; } }

        public bool AgruparJugosIguales { get; set; } = true;
        public bool AgruparBebidasIguales { get; set; } = true;

        private readonly ToolTip _tt = new ToolTip();
        private int _baseHeight;
        private bool _pendingGrow = false;

        private Guna2TextBox _tplJugo;
        private Guna2TextBox _tplBeb;

        // ===== Modelo interno =====
        private sealed class BloqueTexto
        {
            public string Key;
            public string Descripcion;
            public decimal PrecioExtra;
            public int Cantidad;
            public List<string> Notas = new List<string>();
            public Guna2TextBox TextBox;
        }

        private readonly List<BloqueTexto> _jugos = new List<BloqueTexto>();
        private readonly List<BloqueTexto> _bebidas = new List<BloqueTexto>();
        private BloqueTexto _ultimoJugo;
        private BloqueTexto _ultimaBebida;

        private BloqueTexto _bloqueSeleccionado;

        // ===== ILineaSeleccionable =====
        public Control View { get { return this; } }
        public void SetVisualSelected(bool selected) { this.BorderStyle = selected ? BorderStyle.FixedSingle : BorderStyle.None; }

        public ComboPedidoItem()
        {
            InitializeComponent();

            _tplJugo = this.Controls.Find("txtJugoDesayuno", true).OfType<Guna2TextBox>().FirstOrDefault();
            _tplBeb = this.Controls.Find("txtBebCDesay", true).OfType<Guna2TextBox>().FirstOrDefault();

            if (_tplJugo != null) { _tplJugo.Visible = false; if (_tplJugo.Parent is FlowLayoutPanel pj) pj.Controls.Remove(_tplJugo); }
            if (_tplBeb != null) { _tplBeb.Visible = false; if (_tplBeb.Parent is FlowLayoutPanel pb) pb.Controls.Remove(_tplBeb); }

            ConfigurarTextBoxSoloLectura(txtCombo);
            PrepFlow(flpJugos);
            FlowLayoutPanel flpB;
            if (TryFindControl("flpBebCDesayuno", out flpB)) PrepFlow(flpB);

            if (txtCombo != null) txtCombo.TextChanged += (s, e) => RecalcAutoGrowSafe();
            if (flpJugos != null)
            {
                flpJugos.SizeChanged += (s, e) => RecalcAutoGrowSafe();
                flpJugos.ControlAdded += (s, e) => RecalcAutoGrowSafe();
                flpJugos.ControlRemoved += (s, e) => RecalcAutoGrowSafe();
            }
            if (flpB != null)
            {
                flpB.SizeChanged += (s, e) => RecalcAutoGrowSafe();
                flpB.ControlAdded += (s, e) => RecalcAutoGrowSafe();
                flpB.ControlRemoved += (s, e) => RecalcAutoGrowSafe();
            }

            this.SizeChanged += (s, e) => RecalcAutoGrowSafe();

            // selección global
            WireSelectClick(this);
            WireInnerTextBoxClicks(txtCombo);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            int hCombo = (txtCombo != null) ? txtCombo.Height : 0;
            int hJugos = (flpJugos != null) ? flpJugos.Height : 0;

            FlowLayoutPanel flpB;
            int hBeb = (TryFindControl("flpBebCDesayuno", out flpB)) ? flpB.Height : 0;

            _baseHeight = this.Height - (hCombo + hJugos + hBeb);
            RecalcAutoGrowSafe();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_pendingGrow) { _pendingGrow = false; RecalcAutoGrow(); }
        }

        // ===== API Encabezado =====
        public void SetCombo(string codigo, string descripcion, int cantidad, decimal precioUnitarioFinal)
        {
            Codigo = (codigo ?? string.Empty).Trim();
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? Codigo : descripcion.Trim();
            Cantidad = Math.Max(1, cantidad);
            PrecioUnitario = Math.Max(0m, precioUnitarioFinal);

            if (txtCombo != null)
            {
                txtCombo.Text = string.Format("{0} x {1} = S/ {2:0.00}", Cantidad, Descripcion.ToUpperInvariant(), Total);
                _tt.SetToolTip(txtCombo, "PU: S/ " + PrecioUnitario.ToString("0.00"));
            }

            RecalcAutoGrowSafe();
        }

        // ===== API Jugos =====
        public void ClearJugos()
        {
            _jugos.Clear();
            _ultimoJugo = null;
            _bloqueSeleccionado = null;
            if (flpJugos != null) flpJugos.Controls.Clear();
            RecalcAutoGrowSafe();
        }

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
                bloque = new BloqueTexto
                {
                    Key = KeyDe(descripcion, precioExtra),
                    Descripcion = (descripcion ?? "").Trim(),
                    PrecioExtra = precioExtra,
                    Cantidad = 1,
                    TextBox = CrearTextBoxDesdePlantilla(_tplJugo, flpJugos)
                };

                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));

                bloque.TextBox.Text = TextoDe(bloque);
                VincularBloqueVisual(bloque);

                flpJugos.Controls.Add(bloque.TextBox);
                flpJugos.PerformLayout();

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
            RecalcAutoGrowSafe();
        }

        public void AppendNotasAlUltimoJugo(string notas)
        {
            if (_ultimoJugo == null || string.IsNullOrWhiteSpace(notas)) return;
            _ultimoJugo.Notas.AddRange(ToNotas(notas));
            _ultimoJugo.TextBox.Text = TextoDe(_ultimoJugo);
            RecalcAutoGrowSafe();
        }

        public decimal GetExtraPromedioJugosPorUnidad(int cantidadTotal)
        {
            if (cantidadTotal <= 0) return 0m;
            decimal extraTotal = 0m;
            foreach (var b in _jugos) extraTotal += (b.PrecioExtra * b.Cantidad);
            return extraTotal / cantidadTotal;
        }

        // ===== API Bebidas =====
        public void ClearBebidas()
        {
            _bebidas.Clear();
            _ultimaBebida = null;
            _bloqueSeleccionado = null;

            FlowLayoutPanel flpB;
            if (TryFindControl("flpBebCDesayuno", out flpB)) flpB.Controls.Clear();

            RecalcAutoGrowSafe();
        }

        public void AddBebidaUnidad(string descripcion, decimal precioExtra, string notas, bool? forzarIndividual)
        {
            FlowLayoutPanel flpB;
            if (!TryFindControl("flpBebCDesayuno", out flpB)) return;

            bool individual = (forzarIndividual.HasValue) ? forzarIndividual.Value : !AgruparBebidasIguales;

            BloqueTexto bloque = null;
            if (!individual)
            {
                string key = KeyDe(descripcion, precioExtra);
                bloque = _bebidas.FirstOrDefault(x => x.Key == key);
            }

            if (bloque == null)
            {
                bloque = new BloqueTexto
                {
                    Key = KeyDe(descripcion, precioExtra),
                    Descripcion = (descripcion ?? "").Trim(),
                    PrecioExtra = precioExtra,
                    Cantidad = 1,
                    TextBox = CrearTextBoxDesdePlantilla(_tplBeb, flpB)
                };

                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));

                bloque.TextBox.Text = TextoDe(bloque);
                VincularBloqueVisual(bloque);

                flpB.Controls.Add(bloque.TextBox);
                flpB.PerformLayout();

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
            RecalcAutoGrowSafe();
        }

        public void AppendNotasALaUltimaBebida(string notas)
        {
            if (_ultimaBebida == null || string.IsNullOrWhiteSpace(notas)) return;
            _ultimaBebida.Notas.AddRange(ToNotas(notas));
            _ultimaBebida.TextBox.Text = TextoDe(_ultimaBebida);
            RecalcAutoGrowSafe();
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

        // ===== selección global =====
        private void WireSelectClick(Control root)
        {
            if (root == null) return;
            root.Click -= Any_Click_Select; root.Click += Any_Click_Select;
            root.MouseDown -= Any_Click_Select; root.MouseDown += Any_Click_Select;
            foreach (Control c in root.Controls) WireSelectClick(c);
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

        private void Any_Click_Select(object sender, EventArgs e)
        {
            LineaSelection.Select(this, true);   // << selección global única
        }

        // ===== AutoGrow / medidas =====
        private void RecalcAutoGrowSafe()
        {
            if (IsHandleCreated) RecalcAutoGrow();
            else _pendingGrow = true;
        }

        private void RecalcAutoGrow()
        {
            if (!IsHandleCreated) return;

            AjustarAnchosContenedor(flpJugos);
            FlowLayoutPanel flpB2;
            if (TryFindControl("flpBebCDesayuno", out flpB2))
                AjustarAnchosContenedor(flpB2);

            int hCombo = AltoNecesario(txtCombo);
            int hJugos = (flpJugos != null) ? flpJugos.Height : 0;

            int hBeb = 0;
            FlowLayoutPanel flpB3;
            if (TryFindControl("flpBebCDesayuno", out flpB3))
                hBeb = flpB3.Height;

            SuspendLayout();
            if (txtCombo != null && txtCombo.Height != hCombo) txtCombo.Height = hCombo;
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
            var tb = c as Guna2TextBox;
            if (tb == null) return;

            int w = Math.Max(1, tb.ClientSize.Width);
            using (Graphics g = tb.CreateGraphics())
            {
                var sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.MeasureTrailingSpaces);
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
                var sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.MeasureTrailingSpaces);
                var size = g.MeasureString(t + "\nA", tb.Font, w, sf);
                int needed = (int)Math.Ceiling(size.Height) + 4;
                return Math.Max(28, needed);
            }
        }

        // ===== Helpers visuales / edición =====
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

        private Guna2TextBox CrearTextBoxDesdePlantilla(Guna2TextBox plantilla, FlowLayoutPanel contenedor)
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
            int w = (contenedor != null ? contenedor.ClientSize.Width :
                    (this.ClientSize.Width > 0 ? this.ClientSize.Width : 300));
            tb.Width = w;
            tb.MinimumSize = new Size(w, 28);

            tb.Multiline = true;
            tb.WordWrap = true;
            tb.AcceptsReturn = true;
            tb.ScrollBars = ScrollBars.None;
            tb.ReadOnly = true;              // edición via diálogo
            tb.Enabled = true;
            tb.Cursor = Cursors.Hand;
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            tb.TextChanged += (s, e) => RecalcAutoGrowSafe();
            tb.Click += (s, e) =>
            {
                // marca bloque y selecciona item
                _bloqueSeleccionado = (BloqueTexto)((Control)s).Tag;
                LineaSelection.Select(this, true);
            };

            return tb;
        }

        private static string KeyDe(string descripcion, decimal precio)
        {
            string d = (descripcion ?? "").Trim().ToUpperInvariant();
            return d + "|" + precio.ToString("0.00", CultureInfo.InvariantCulture);
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

            if (b.Cantidad <= 1) sb.Append("1 x ").Append(b.Descripcion);
            else sb.Append(b.Cantidad).Append(" x ").Append(b.Descripcion);

            decimal totalExtra = b.PrecioExtra * b.Cantidad;
            if (totalExtra > 0m)
                sb.Append(" = S/ ").Append(totalExtra.ToString("0.00", CultureInfo.InvariantCulture));

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
            catch { return null; }
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

        private void VincularBloqueVisual(BloqueTexto bloque)
        {
            var tb = bloque.TextBox;
            tb.Tag = bloque;

            tb.Click += (s, e) =>
            {
                _bloqueSeleccionado = (BloqueTexto)((Control)s).Tag;
                LineaSelection.Select(this, true);
            };

            tb.DoubleClick += (s, e) =>
            {
                _bloqueSeleccionado = (BloqueTexto)((Control)s).Tag;
                EditarNotasSeleccionadas(this.FindForm());
            };
        }

        private static string FormatearNotas(IEnumerable<string> notas)
        {
            var arr = (notas ?? new List<string>())
                      .Select(n => n == null ? "" : n.Trim())
                      .Where(n => n.Length > 0)
                      .Select(n => n.StartsWith("-") ? n : "- " + n);
            return string.Join(Environment.NewLine, arr);
        }

        private static List<string> ParseNotas(string raw)
        {
            return ToNotas(raw).ToList();
        }

        public bool EditarNotasSeleccionadas(IWin32Window owner)
        {
            if (_bloqueSeleccionado == null) return false;

            using (var frm = new CapaPresentacion.frmComentarioLbr())
            {
                frm.Texto = FormatearNotas(_bloqueSeleccionado.Notas);
                frm.TextoInicial = frm.Texto;

                if (frm.ShowDialog(owner) == DialogResult.OK)
                {
                    _bloqueSeleccionado.Notas = ParseNotas(frm.Comentario);
                    _bloqueSeleccionado.TextBox.Text = TextoDe(_bloqueSeleccionado);
                    RecalcAutoGrowSafe();
                    return true;
                }
            }
            return false;
        }

        public bool EditarUltimoJugoOBebida(IWin32Window owner)
        {
            var b = _bloqueSeleccionado
                 ?? _ultimoJugo
                 ?? _ultimaBebida
                 ?? (_jugos.Count > 0 ? _jugos[_jugos.Count - 1] : null)
                 ?? (_bebidas.Count > 0 ? _bebidas[_bebidas.Count - 1] : null);

            if (b == null) return false;

            _bloqueSeleccionado = b;
            return EditarNotasSeleccionadas(owner);
        }
    }
}
