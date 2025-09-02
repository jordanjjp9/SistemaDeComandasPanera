using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace CapaPresentacion.Controles
{
    public partial class LineaPedidoItem : UserControl
    {
        // ======== Selección global (exclusiva) ========
        public static LineaPedidoItem SeleccionActual { get; private set; }
        public static event EventHandler SeleccionCambio;

        // ======== Datos de la línea ========
        public string Codigo { get; private set; } = "";
        public string Descripcion { get; private set; } = "";
        public int Cantidad { get; private set; } = 1;
        public decimal PrecioUnitario { get; private set; } = 0m;
        public decimal Importe { get { return Cantidad * PrecioUnitario; } }

        // ======== UI / estado ========
        private readonly ToolTip _tt = new ToolTip();
        private int _baseHeight;                 // alto del control sin el alto de producto+nota
        private bool _pendingGrow = false;       // para recalcular cuando aún no hay handle

        public LineaPedidoItem()
        {
            InitializeComponent();

            // Apariencia base
            this.BorderStyle = BorderStyle.None;
            this.BackColor = Color.WhiteSmoke;

            // ---- txtProducto (Guna2TextBox) ----
            txtProducto.Multiline = true;
            txtProducto.WordWrap = true;
            txtProducto.AcceptsReturn = true;
            txtProducto.ScrollBars = ScrollBars.None;
            txtProducto.ReadOnly = true;
            txtProducto.Enabled = true;               // ¡no lo deshabilites!
            txtProducto.Cursor = Cursors.Hand;

            // ---- txtNote (Guna2TextBox) ----
            txtNote.Multiline = true;
            txtNote.WordWrap = true;
            txtNote.AcceptsReturn = true;
            txtNote.ScrollBars = ScrollBars.None;
            txtNote.ReadOnly = true;
         //   txtNote.Enabled = true;                   // ¡no lo deshabilites!
            txtNote.Cursor = Cursors.Hand;

            // Calcula base una vez que el layout sea real
            this.Load += (s, e) =>
            {
                _baseHeight = this.Height - txtProducto.Height - txtNote.Height;
                BeginInvoke(new Action(RecalcAutoGrow));
            };

            // AutoGrow al cambiar textos o tamaño del control
            txtProducto.TextChanged += (s, e) => RecalcAutoGrow();
            txtNote.TextChanged += (s, e) => RecalcAutoGrow();
            this.SizeChanged += (s, e) => RecalcAutoGrow();

            // Selección: clic en cualquier parte del control o de sus hijos
            WireSelectClick(this);
        }

        // ========= API =========

        public void Configurar(string codigo, string descripcion, int cantidad, decimal precioUnitario, string notas)
        {
            Codigo = (codigo ?? string.Empty).Trim();
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? Codigo : descripcion.Trim();
            Cantidad = Math.Max(1, cantidad);
            PrecioUnitario = Math.Max(0m, precioUnitario);

            txtProducto.Text = string.Format("{0} x {1} = S/ {2:0.00}",
                Cantidad, Descripcion.ToUpperInvariant(), Importe);

            Notas = notas; // asigna y recalcula

            _tt.SetToolTip(txtProducto, string.Format("PU: S/ {0:0.00}", PrecioUnitario));
        }

        public string Notas
        {
            get { return (txtNote != null ? txtNote.Text : string.Empty).TrimEnd(); }
            set
            {
                if (txtNote == null) return;

                string s = (value ?? string.Empty)
                           .Replace("\r\n", "\n")
                           .Replace("\n", Environment.NewLine);

                txtNote.Text = s;

                if (IsHandleCreated)
                {
                    BeginInvoke(new Action(RecalcAutoGrow));
                }
                else
                {
                    _pendingGrow = true;
                }
            }
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

        // ========= Selección =========

        private void WireSelectClick(Control root)
        {
            root.Click -= Any_Click_Select;
            root.Click += Any_Click_Select;

            root.MouseDown -= Any_Click_Select;
            root.MouseDown += Any_Click_Select;

            // También engancha el TextBox interno de Guna2TextBox
            var innerProd = TryGetInnerTextBox(txtProducto);
            if (innerProd != null)
            {
                innerProd.Click -= Any_Click_Select;
                innerProd.Click += Any_Click_Select;
                innerProd.MouseDown -= Any_Click_Select;
                innerProd.MouseDown += Any_Click_Select;
            }

            var innerNote = TryGetInnerTextBox(txtNote);
            if (innerNote != null)
            {
                innerNote.Click -= Any_Click_Select;
                innerNote.Click += Any_Click_Select;
                innerNote.MouseDown -= Any_Click_Select;
                innerNote.MouseDown += Any_Click_Select;
            }

            // Clic en hijos
            foreach (Control c in root.Controls)
                WireSelectClick(c);
        }

        private void Any_Click_Select(object sender, EventArgs e)
        {
            SeleccionarEste();
        }

        private void SeleccionarEste()
        {
            if (SeleccionActual == this) return;

            var anterior = SeleccionActual;
            SeleccionActual = this;

            if (anterior != null) anterior.SetVisualSelected(false);
            this.SetVisualSelected(true);

            var handler = SeleccionCambio;
            if (handler != null) handler.Invoke(null, EventArgs.Empty);
        }

        public void SetVisualSelected(bool selected)
        {
            this.BorderStyle = selected ? BorderStyle.FixedSingle : BorderStyle.None;
        }

        // ========= AutoGrow (producto + notas) =========

        private void RecalcAutoGrow()
        {
            if (!IsHandleCreated) return;

            int hProd = AltoNecesario(txtProducto);
            int hNote = AltoNecesario(txtNote);

            SuspendLayout();
            if (txtProducto.Height != hProd) txtProducto.Height = hProd;
            if (txtNote.Height != hNote) txtNote.Height = hNote;

            this.Height = _baseHeight + hProd + hNote;
            ResumeLayout();
        }

        private int AltoNecesario(Control txt)
        {
            if (txt == null) return 0;

            // Texto actual sin perder CR/LF
            string t = Convert.ToString(txt.GetType().GetProperty("Text").GetValue(txt, null)) ?? string.Empty;

            // Ancho disponible
            int w = txt.ClientSize.Width > 0 ? txt.ClientSize.Width : Math.Max(1, txt.Width - 6);

            // Si está vacío, mínimo
            if (t.Length == 0)
                return Math.Max(24, txt.Font.Height + 6);

            // 1) Si es Guna2TextBox y podemos acceder al TextBox interno, contamos líneas exactas
            TextBox inner = TryGetInnerTextBox(txt);
            if (inner != null && inner.IsHandleCreated)
            {
                int last = t.Length - 1;
                // Evita medir CR/LF finales (subestima una línea)
                while (last >= 0 && (t[last] == '\r' || t[last] == '\n')) last--;
                if (last < 0) last = 0;

                // Asegura wrap activado en el interno, por si acaso
                try { inner.WordWrap = true; } catch { }

                Point pt = inner.GetPositionFromCharIndex(last);
                int needed = pt.Y + inner.Font.Height + 6; // padding inferior
                return Math.Max(24, needed);
            }

            // 2) Fallback genérico midiendo el texto dibujado (+“\nA” para no perder la última línea)
            using (Graphics g = txt.CreateGraphics())
            {
                StringFormat sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.NoClip);
                sf.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;

                SizeF size = g.MeasureString(t + "\nA", txt.Font, w, sf);
                int needed = (int)Math.Ceiling(size.Height) + 4;
                return Math.Max(24, needed);
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

        public static void Seleccionar(LineaPedidoItem item, bool scrollIntoView = true)
        {
            if (SeleccionActual == item) return;

            // Desmarca el anterior
            if (SeleccionActual != null)
                SeleccionActual.SetVisualSelected(false);

            // Marca el nuevo
            SeleccionActual = item;
            if (SeleccionActual != null)
            {
                SeleccionActual.SetVisualSelected(true);

                if (scrollIntoView)
                {
                    // Busca el primer ancestro que sea ScrollableControl con AutoScroll
                    ScrollableControl sc = GetScrollableAncestor(SeleccionActual);
                    sc?.ScrollControlIntoView(SeleccionActual);
                }
            }

            SeleccionCambio?.Invoke(null, EventArgs.Empty);
        }

        private static ScrollableControl GetScrollableAncestor(Control c)
        {
            for (Control p = c?.Parent; p != null; p = p.Parent)
                if (p is ScrollableControl s && s.AutoScroll)
                    return s;
            return null;
        }
    }
}
    