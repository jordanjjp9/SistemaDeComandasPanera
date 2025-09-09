using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion.Notas
{
    public partial class frmNWaffles : Form
    {
        public string TextoInicial { get; set; } = string.Empty;
        public string ProductoBaseTexto { get; set; } = string.Empty;
        public string Notas { get; private set; } = string.Empty;
        private readonly HashSet<Control> _chipsWired = new HashSet<Control>();

        private TextBoxBase _txtNotas;
        private Control _txtNotasCtrl;

        private Control _btnContinuar;
        private Control _btnEliminar;
        public frmNWaffles()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            this.Load += frmNWaffles_Load;
        }

        private void frmNWaffles_Load(object sender, EventArgs e)
        {
            // 1) Resolver el área de notas
            _txtNotas = FindNotasTextBox();
            if (_txtNotas == null)
                _txtNotasCtrl = FindNotasControl();

            // 2) Botones principales (resolver como Control, no OfType<Button>())
            _btnContinuar = this.Controls.Find("btnContinuar", true).FirstOrDefault();
            _btnEliminar = this.Controls.Find("btnEliminar", true).FirstOrDefault();

            if (_btnContinuar != null)
            {
                // Asegura que NO quede cableado como chip
                _btnContinuar.Click -= Chip_Click;

                _btnContinuar.Click -= btnContinuar_Click;
                _btnContinuar.Click += btnContinuar_Click;

                // Si implementa IButtonControl (Guna2Button lo hace), úsalo como AcceptButton
                if (_btnContinuar is IButtonControl ib)
                    this.AcceptButton = ib;
            }
            if (_btnEliminar != null)
            {
                // Asegura que NO quede cableado como chip
                _btnEliminar.Click -= Chip_Click;

                _btnEliminar.Click -= btnEliminar_Click;
                _btnEliminar.Click += btnEliminar_Click;
            }

            // 3) Encabezado con producto base (opcional)
            var txtProd = this.Controls.Find("txtProductoSelect", true).FirstOrDefault();
            if (txtProd != null && !string.IsNullOrWhiteSpace(ProductoBaseTexto))
                TrySetText(txtProd, ProductoBaseTexto);

            // 4) Precarga de notas (opcional)
            if (!string.IsNullOrEmpty(TextoInicial))
                TrySetNotasText(TextoInicial);

            // 5) Enganchar TODOS los “chips” (botones), excluyendo Continuar/Eliminar
            WireQuickNoteButtonsRecursive(this);
        }
        private void WireQuickNoteButtonsRecursive(Control root)
        {
            if (root == null) return;

            // Recorre hijos primero
            foreach (Control c in root.Controls)
                WireQuickNoteButtonsRecursive(c);

            // 🔒 No tocar nunca el área de notas
            if (ReferenceEquals(root, _txtNotas) || ReferenceEquals(root, _txtNotasCtrl)) return;
            if (root is TextBoxBase) return;
            if ((root.GetType().Name ?? "").IndexOf("TextBox", StringComparison.OrdinalIgnoreCase) >= 0) return;

            // Excluir botones de acción (Continuar/Eliminar)
            if (EsAccion(root)) return;

            // ¿Es un chip/opción rápida?
            if (EsBotonOpcion(root))
            {
                if (_chipsWired.Add(root))        // evita múltiples Click +=
                    root.Click += Chip_Click;
            }
        }
        private static bool EsBotonOpcion(Control c)
        {
            if (c == null) return false;

            // Nunca un textbox
            if (c is TextBoxBase) return false;
            var tn = c.GetType().Name ?? "";
            if (tn.IndexOf("TextBox", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            // Botones típicos (Button, Guna2Button, etc.)
            if (c is Button) return true;
            if (tn.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            // Heurística final por nombre
            return (c.Name ?? "").StartsWith("btn", StringComparison.OrdinalIgnoreCase);
        }
        private bool EsAccion(Control c)
        {
            if (c == null) return false;

            // Por referencia
            if (ReferenceEquals(c, _btnContinuar) || ReferenceEquals(c, _btnEliminar))
                return true;

            // Por nombre (respaldo si no los encontró)
            var n = (c.Name ?? "").ToLowerInvariant();
            if (n == "btncontinuar" || n == "btneliminar")
                return true;

            // Por Tag (si quieres marcarlo en el diseñador)
            if (string.Equals(c.Tag as string, "accion", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
        private void WireChipButtons(Control root, Control btnContinuar, Control btnEliminar)
        {
            if (root == null) return;
            foreach (Control c in root.Controls)
            {
                WireChipButtons(c, btnContinuar, btnEliminar);

                // botón "chip"
                bool esBtn = (c is Button) ||
                             (c.GetType().Name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0) ||
                             ((c.Name ?? "").StartsWith("btn", StringComparison.OrdinalIgnoreCase));

                if (!esBtn) continue;
                if (ReferenceEquals(c, btnContinuar) || ReferenceEquals(c, btnEliminar)) continue;

                c.Click -= Chip_Click;
                c.Click += Chip_Click;
            }
        }
        private void Chip_Click(object sender, EventArgs e)
        {
            var c = sender as Control;
            if (c == null) return;

            var texto = (c.Text ?? "").Trim();
            if (texto.Length == 0) return;

            string actual = ReadNotas() ?? string.Empty;
            if (actual.Length > 0 && !actual.EndsWith(Environment.NewLine))
                actual += Environment.NewLine;

            actual += "- " + texto + Environment.NewLine;
            WriteNotas(actual);
        }
        private void btnContinuar_Click(object sender, EventArgs e)
        {
            Notas = ReadNotas() ?? string.Empty;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            var lines = (ReadNotas() ?? string.Empty).Replace("\r\n", "\n").Split('\n');
            if (lines.Length == 0) return;
            int i = lines.Length - 1;
            while (i >= 0 && string.IsNullOrWhiteSpace(lines[i])) i--;
            if (i < 0) return;

            var nuevas = (i == 0) ? new string[0] : lines.Take(i).ToArray();
            WriteNotas(string.Join(Environment.NewLine, nuevas));
        }
        private void TrySetNotasText(string s)
        {
            if (s == null) s = string.Empty;

            if (_txtNotas != null)
            {
                // TextBoxBase (TextBox / RichTextBox)
                _txtNotas.Text = s;

                // ⚠️ limpiar selección y colocar caret al final
                _txtNotas.SelectionStart = _txtNotas.TextLength;
                _txtNotas.SelectionLength = 0;        // <--- clave
                _txtNotas.HideSelection = true;       // opcional: no mostrar selección cuando pierde el foco
                _txtNotas.ScrollToCaret();
                _txtNotas.Focus();
            }
            else if (_txtNotasCtrl != null)
            {
                // Otros (p.ej. Guna2TextBox)
                _txtNotasCtrl.Text = s;
                SetCaretToEnd(_txtNotasCtrl);         // <--- coloca caret y deselecciona
                _txtNotasCtrl.Focus();
            }
        }
        private static void TrySetText(Control any, string text)
        {
            try
            {
                var p = any.GetType().GetProperty("Text");
                p?.SetValue(any, text, null);
            }
            catch { }
        }
        private TextBoxBase FindNotasTextBox()
        {
            var candidatos = new[]
            {
                "txtNotasBebida", "txtNotasBCalient", "txtNBebidas",
                "txtNotas", "txtNota", "txtComentarios", "txtComentLibr","txtNotasWaffles"
            };

            foreach (var name in candidatos)
            {
                var tb = this.Controls.Find(name, true).OfType<TextBoxBase>().FirstOrDefault();
                if (tb != null) return tb;
            }

            return this.Controls.OfType<TextBoxBase>().FirstOrDefault(t => t.Multiline);
        }
        private Control FindNotasControl()
        {
            var candidatos = new[]
            {
                "txtNotasBebida", "txtNotasBCalient", "txtNBebidas",
                "txtNotas", "txtNota", "txtComentarios", "txtComentLibr","txtNotasWaffles"
            };

            foreach (var name in candidatos)
            {
                var c = this.Controls.Find(name, true).FirstOrDefault();
                if (c != null) return c;
            }

            // Fallback: primer control con propiedad Text
            return this.Controls.Cast<Control>()
                                .FirstOrDefault(ctrl => ctrl.GetType().GetProperty("Text") != null);
        }
        private string ReadNotas()
        {
            if (_txtNotas != null) return _txtNotas.Text;
            if (_txtNotasCtrl != null) return _txtNotasCtrl.Text ?? string.Empty;
            return string.Empty;
        }
        private void WriteNotas(string s)
        {
            if (_txtNotas != null) { _txtNotas.Text = s ?? string.Empty; _txtNotas.SelectionStart = _txtNotas.TextLength; _txtNotas.ScrollToCaret(); }
            else if (_txtNotasCtrl != null) _txtNotasCtrl.Text = s ?? string.Empty;
        }
        private static void SetCaretToEnd(Control c)
        {
            try
            {
                // Si el control ya expone SelectionStart/SelectionLength, úsalos
                var pSelStart = c.GetType().GetProperty("SelectionStart");
                var pSelLen = c.GetType().GetProperty("SelectionLength");
                var pText = c.GetType().GetProperty("Text");

                if (pSelStart != null && pSelLen != null && pText != null)
                {
                    int len = ((string)pText.GetValue(c))?.Length ?? 0;
                    pSelStart.SetValue(c, len, null);
                    pSelLen.SetValue(c, 0, null);
                    return;
                }

                // Guna2TextBox tiene un TextBox interno accesible como propiedad "TextBox"
                var innerProp = c.GetType().GetProperty("TextBox",
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);

                var inner = innerProp?.GetValue(c) as TextBoxBase;
                if (inner != null)
                {
                    inner.SelectionStart = inner.TextLength;
                    inner.SelectionLength = 0;
                    inner.ScrollToCaret();
                }
            }
            catch { /* ignore */ }
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
