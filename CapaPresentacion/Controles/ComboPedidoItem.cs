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
using Guna.UI2.WinForms;
using Guna.UI2.WinForms.Suite;

namespace CapaPresentacion.Controles
{
    public partial class ComboPedidoItem : UserControl
    {
        // ======== Selección global (exclusiva para combos) ========
        public static ComboPedidoItem SeleccionActual { get; private set; }
        public static event EventHandler SeleccionCambio;

        // ======== Datos del combo ========
        public string Codigo { get; private set; } = "";
        public string Descripcion { get; private set; } = "";
        public int Cantidad { get; private set; } = 1;
        public decimal PrecioUnitario { get; private set; } = 0m;   // PU final (con extra promedio)
        public decimal Total => Cantidad * PrecioUnitario;

        // Accesos de solo lectura
        public string TextoCombo => txtCombo != null ? txtCombo.Text : string.Empty;

        // ======== UI / estado ========
        private readonly ToolTip _tt = new ToolTip();
        private int _baseHeight;                 // alto del control sin contar txtCombo ni flpJugos
        private bool _pendingGrow = false;       // para recalcular cuando aún no hay handle

        // Plantilla opcional para clonar estilo de los jugos (si existe en diseñador)
        private Guna2TextBox _tplJugo;

        // Contenedor dinámico de jugos (debe existir en diseñador; si no, lo creo en runtime)
        private FlowLayoutPanel _flpJugos;

        // ======== Modelo interno de cada bloque de jugo ========
        private sealed class JugoBlock
        {
            public string Key;                        // Descripcion(normalizada) + "|" + PrecioExtra
            public string Descripcion;
            public decimal PrecioExtra;
            public int Cantidad;                      // cuántas unidades agrupa
            public List<string> Notas = new List<string>();
            public Guna2TextBox TextBox;              // textbox visual editable
        }

        private readonly List<JugoBlock> _jugos = new List<JugoBlock>();
        private JugoBlock _ultimoBloque;

        public ComboPedidoItem()
        {
            InitializeComponent();

            // Configura cabecera
            ConfigurarTextBoxSoloLectura(txtCombo);

            // Localiza contenedor de jugos
            _flpJugos = this.Controls.Find("flpJugos", true).OfType<FlowLayoutPanel>().FirstOrDefault();
            if (_flpJugos == null)
            {
                // Fallback: si no existe en diseñador, lo creo (para no romper)
                _flpJugos = new FlowLayoutPanel
                {
                    Name = "flpJugos",
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    Dock = DockStyle.Top,
                    Margin = new Padding(0),
                    Padding = new Padding(0)
                };
                this.Controls.Add(_flpJugos);
                _flpJugos.BringToFront();
            }

            // Intenta capturar plantilla de jugo (si existe un txtJugoDesayuno en el diseñador)
            _tplJugo = this.Controls.Find("txtJugoDesayuno", true).OfType<Guna2TextBox>().FirstOrDefault();
            if (_tplJugo != null)
            {
                _tplJugo.Visible = false; // se usa solo como plantilla
            }

            // Calcula baseHeight cuando el layout sea real
            this.Load += (s, e) =>
            {
                int hCombo = txtCombo != null ? txtCombo.Height : 0;
                int hJugos = _flpJugos != null ? _flpJugos.Height : 0;

                _baseHeight = this.Height - (hCombo + hJugos);
                BeginInvoke(new Action(RecalcAutoGrow));
            };

            // Reajustes de altura
            if (txtCombo != null) txtCombo.TextChanged += (s, e) => RecalcAutoGrow();
            if (_flpJugos != null) _flpJugos.SizeChanged += (s, e) => RecalcAutoGrow();
            this.SizeChanged += (s, e) => RecalcAutoGrow();

            // Selección exclusiva: clic en cualquier parte (y en TextBox interno)
            WireSelectClick(this);
            WireInnerTextBoxClicks(txtCombo);
        }

        // ========= API pública =========

        /// <summary>
        /// Establece el encabezado del combo y su PU. El texto queda como: "N x DESCRIPCION = S/ total".
        /// </summary>
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

            // Asegura recálculo
            if (IsHandleCreated) BeginInvoke(new Action(RecalcAutoGrow));
            else _pendingGrow = true;
        }

        /// <summary>
        /// Limpia todos los jugos del combo.
        /// </summary>
        public void ClearJugos()
        {
            _jugos.Clear();
            _ultimoBloque = null;
            if (_flpJugos != null) _flpJugos.Controls.Clear();
            RecalcAutoGrow();
        }

        /// <summary>
        /// Agrega una unidad de jugo. Si coincide con uno ya existente (misma descripción y precio extra), se agrupa.
        /// Las notas (líneas) se agregan al bloque correspondiente.
        /// </summary>
        public void AddJugoUnidad(string descripcion, decimal precioExtra, string notas)
        {
            if (_flpJugos == null) return;

            string key = KeyDe(descripcion, precioExtra);
            var bloque = _jugos.FirstOrDefault(x => x.Key == key);

            if (bloque == null)
            {
                bloque = new JugoBlock
                {
                    Key = key,
                    Descripcion = (descripcion ?? "").Trim(),
                    PrecioExtra = precioExtra,
                    Cantidad = 1,
                    TextBox = CrearTextBoxJugo()
                };

                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));

                bloque.TextBox.Text = TextoDe(bloque);

                _flpJugos.Controls.Add(bloque.TextBox);
                _jugos.Add(bloque);
            }
            else
            {
                // Agrupar
                bloque.Cantidad += 1;
                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));

                bloque.TextBox.Text = TextoDe(bloque);
            }

            _ultimoBloque = bloque;
            RecalcAutoGrow();
        }

        /// <summary>
        /// Agrega líneas de notas al último bloque de jugo creado o utilizado.
        /// </summary>
        public void AppendNotasAlUltimoJugo(string notas)
        {
            if (_ultimoBloque == null || string.IsNullOrWhiteSpace(notas)) return;
            _ultimoBloque.Notas.AddRange(ToNotas(notas));
            _ultimoBloque.TextBox.Text = TextoDe(_ultimoBloque);
            RecalcAutoGrow();
        }

        /// <summary>
        /// Devuelve el extra promedio por unidad de desayuno (para ajustar el PU final).
        /// </summary>
        public decimal GetExtraPromedioPorUnidad(int cantidadTotal)
        {
            if (cantidadTotal <= 0) return 0m;
            decimal extraTotal = 0m;
            foreach (var b in _jugos)
                extraTotal += (b.PrecioExtra * b.Cantidad);
            return extraTotal / cantidadTotal;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_pendingGrow)
            {
                _pendingGrow = false;
                BeginInvoke(new Action(RecalcAutoGrow));
            }
        }

        // ========= Selección (exclusiva) =========

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

            // txtCombo es solo lectura: el click selecciona el panel
            inner.ReadOnly = true;
            inner.ShortcutsEnabled = false;
            inner.Cursor = Cursors.Hand;

            inner.Click -= Any_Click_Select; inner.Click += Any_Click_Select;
            inner.MouseDown -= Any_Click_Select; inner.MouseDown += Any_Click_Select;
        }

        private void Any_Click_Select(object sender, EventArgs e)
        {
            SeleccionarEste();
        }

        public void SeleccionarEste()
        {
            if (SeleccionActual == this) return;

            var anterior = SeleccionActual;
            SeleccionActual = this;

            if (anterior != null) anterior.SetVisualSelected(false);
            this.SetVisualSelected(true);

            // Asegura visibilidad en su contenedor scrollable
            var scrollable = this.Parent as ScrollableControl;
            if (scrollable != null) scrollable.ScrollControlIntoView(this);

            var handler = SeleccionCambio;
            if (handler != null) handler.Invoke(null, EventArgs.Empty);
        }

        public void SetVisualSelected(bool selected)
        {
            this.BorderStyle = selected ? BorderStyle.FixedSingle : BorderStyle.None;
        }

        // ========= AutoGrow =========

        private void RecalcAutoGrow()
        {
            if (!IsHandleCreated) return;

            int hCombo = AltoNecesario(txtCombo);
            int hJugos = (_flpJugos != null) ? _flpJugos.Height : 0;

            SuspendLayout();
            if (txtCombo != null && txtCombo.Height != hCombo) txtCombo.Height = hCombo;

            // La altura total es base + cabecera + contenedor de jugos (AutoSize)
            this.Height = _baseHeight + hCombo + hJugos;
            ResumeLayout();
        }

        private int AltoNecesario(Control txt)
        {
            if (txt == null) return 0;

            string t = Convert.ToString(txt.GetType().GetProperty("Text").GetValue(txt, null)) ?? string.Empty;

            // Ancho disponible
            int w = txt.ClientSize.Width > 0 ? txt.ClientSize.Width : Math.Max(1, txt.Width - 6);

            // Si está vacío, usa un mínimo
            if (t.Length == 0)
                return Math.Max(24, txt.Font.Height + 6);

            // 1) Si es Guna2TextBox y podemos acceder al TextBox interno, contamos líneas exactas
            TextBox inner = TryGetInnerTextBox(txt);
            if (inner != null && inner.IsHandleCreated)
            {
                int last = t.Length - 1;
                while (last >= 0 && (t[last] == '\r' || t[last] == '\n')) last--;
                if (last < 0) last = 0;

                try { inner.WordWrap = true; } catch { }

                Point pt = inner.GetPositionFromCharIndex(last);
                int needed = pt.Y + inner.Font.Height + 6; // padding inferior
                return Math.Max(24, needed);
            }

            // 2) Fallback genérico (mide dibujo; añade "\nA" para no perder última línea)
            using (Graphics g = txt.CreateGraphics())
            {
                StringFormat sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.NoClip);
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                SizeF size = g.MeasureString(t + "\nA", txt.Font, w, sf);
                int needed = (int)Math.Ceiling(size.Height) + 4;
                return Math.Max(24, needed);
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
            tb.ReadOnly = true;      // encabezado del combo NO editable
            tb.Enabled = true;
            tb.Cursor = Cursors.Hand;
        }

        private Guna2TextBox CrearTextBoxJugo()
        {
            Guna2TextBox tb;

            if (_tplJugo != null)
            {
                tb = new Guna2TextBox
                {
                    Font = _tplJugo.Font,
                    ForeColor = _tplJugo.ForeColor,
                    FillColor = _tplJugo.FillColor,
                    BorderRadius = _tplJugo.BorderRadius,
                    BorderColor = _tplJugo.BorderColor,
                    BorderThickness = _tplJugo.BorderThickness,
                    Padding = _tplJugo.Padding,
                    Margin = new Padding(0, 4, 0, 0)
                };
            }
            else
            {
                tb = new Guna2TextBox
                {
                    BorderRadius = 6,
                    BorderThickness = 1,
                    Margin = new Padding(0, 4, 0, 0)
                };
            }

            // Jugos: EDITABLES (para que el mozo pueda ajustar notas)
            tb.Multiline = true;
            tb.WordWrap = true;
            tb.AcceptsReturn = true;
            tb.ScrollBars = ScrollBars.None;
            tb.ReadOnly = false;
            tb.Enabled = true;
            tb.Cursor = Cursors.IBeam;

            // Al teclear, recalcula altura total del control
            tb.TextChanged += (s, e) => RecalcAutoGrow();

            // Click selecciona el panel
            tb.Click += (s, e) => SeleccionarEste();

            return tb;
        }

        private static string KeyDe(string descripcion, decimal precio)
        {
            var d = (descripcion ?? "").Trim().ToUpperInvariant();
            return d + "|" + precio.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string TextoDe(JugoBlock b)
        {
            var sb = new StringBuilder();

            // Encabezado del bloque: "N x DESCRIPCION (= S/ 0.00 si aplica)"
            if (b.Cantidad <= 1)
                sb.Append("1 x ").Append(b.Descripcion);
            else
                sb.Append(b.Cantidad).Append(" x ").Append(b.Descripcion);

            if (b.PrecioExtra > 0)
                sb.Append(" = S/ ").Append(b.PrecioExtra.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

            // Notas
            foreach (var n in b.Notas)
                sb.AppendLine().Append("  - ").Append(n);

            return sb.ToString();
        }

        private static IEnumerable<string> ToNotas(string notas)
        {
            var t = (notas ?? "").Replace("\r\n", "\n");
            var parts = t.Split('\n');
            foreach (var s0 in parts)
            {
                var s = s0.Trim();
                if (s.Length == 0) continue;
                if (s.StartsWith("-")) s = s.TrimStart('-').Trim();
                yield return s;
            }
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
    }
}
