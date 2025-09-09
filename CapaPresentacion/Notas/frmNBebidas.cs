using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CapaPresentacion.Notas
{
    public partial class frmNBebidas : Form
    {
        // ======== Entradas / salidas ========
        public string ProductoBaseTexto { get; set; } = string.Empty;
        public string TextoInicial { get; set; } = string.Empty;
        public string Notas { get; private set; } = string.Empty;
        // frmNBebidas (campos privados)
        private readonly HashSet<Control> _chipsWired = new HashSet<Control>();


        /// <summary>Cuántas bebidas calientes se omiten porque aquí se eligió “GRANDE”. (0 o 1)</summary>
        public int CuposCalienteConsumidos { get; private set; } = 0;

        // ======== Internos ========
        private TextBoxBase _txtNotas;     // TextBox / RichTextBox real si existe
        private Control _txtNotasCtrl;     // fallback (p. ej., Guna2TextBox)
        private readonly List<bool> _lineaConsumeCupo = new List<bool>(); // para deshacer “GRANDE” con Eliminar

        // Botones de acción (pueden ser Guna2Button, etc.)
        private Control _btnContinuar;
        private Control _btnEliminar;

        // Identificación del botón "GRANDE"
        private const string NAME_GRANDE = "btnGrd";
        private const string TEXT_GRANDE = "GRANDE";

        public frmNBebidas()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            Load += frmNBebidas_Load;
        }

        private void frmNBebidas_Load(object sender, EventArgs e)
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

        // ================== Botones principales ==================
        private void btnContinuar_Click(object sender, EventArgs e)
        {
            Notas = ReadNotasText();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            // Elimina la última línea no vacía y, si consumía cupo (GRANDE), lo devuelve
            var lines = GetLines();
            if (lines.Length == 0) return;

            int i = lines.Length - 1;
            while (i >= 0 && string.IsNullOrWhiteSpace(lines[i])) i--;
            if (i < 0) return;

            bool consumia = false;
            if (_lineaConsumeCupo.Count > 0)
            {
                consumia = _lineaConsumeCupo[_lineaConsumeCupo.Count - 1];
                _lineaConsumeCupo.RemoveAt(_lineaConsumeCupo.Count - 1);
            }

            var nuevas = (i == 0) ? Array.Empty<string>() : lines.Take(i).ToArray();
            SetLines(nuevas);

            if (consumia && CuposCalienteConsumidos > 0)
                CuposCalienteConsumidos--;
        }

        // ================== Chips (opciones rápidas) ==================
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

        private void Chip_Click(object sender, EventArgs e)
        {
            var c = sender as Control;
            if (c == null || EsAccion(c)) return; // seguridad extra

            if (ReferenceEquals(c, _txtNotas) || ReferenceEquals(c, _txtNotasCtrl)) return;

            bool esGrande = IsGrandeControl(c);
            if (esGrande)
            {
                if (CuposCalienteConsumidos >= 1)
                {
                    System.Media.SystemSounds.Beep.Play();
                    return; // solo se permite un GRANDE
                }
                CuposCalienteConsumidos += 1;
            }

            string actual = ReadNotasText() ?? string.Empty;
            if (actual.Length > 0 && !actual.EndsWith(Environment.NewLine))
                actual += Environment.NewLine;

            string textoChip = (c.Text ?? string.Empty).Trim();
            if (textoChip.Length == 0) return;

            string nuevaLinea = "- " + textoChip;

            TrySetNotasText(actual + nuevaLinea + Environment.NewLine);

            _lineaConsumeCupo.Add(esGrande);
        }

        // ================== Helpers de texto ==================
        private string ReadNotasText()
        {
            if (_txtNotas != null) return _txtNotas.Text;
            if (_txtNotasCtrl != null) return _txtNotasCtrl.Text ?? string.Empty;
            return string.Empty;
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

        private string[] GetLines()
        {
            var t = ReadNotasText() ?? string.Empty;
            t = t.Replace("\r\n", "\n").Replace("\r", "\n");
            return t.Split('\n');
        }

        private void SetLines(string[] lines)
        {
            string joined = string.Join(Environment.NewLine, lines ?? Array.Empty<string>());
            TrySetNotasText(joined);
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

        // ================== Localización de controles ==================
        private TextBoxBase FindNotasTextBox()
        {
            var candidatos = new[]
            {
                "txtNotasBebida", "txtNotasBCalient", "txtNBebidas",
                "txtNotas", "txtNota", "txtComentarios", "txtComentLibr"
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
                "txtNotas", "txtNota", "txtComentarios", "txtComentLibr"
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

        // ================== Detección de “GRANDE” ==================
        private static bool IsGrandeControl(Control c)
        {
            if (c == null) return false;

            if (string.Equals(c.Name, NAME_GRANDE, StringComparison.OrdinalIgnoreCase))
                return true;

            var txt = (c.Text ?? string.Empty).Trim().ToUpperInvariant();
            if (txt == TEXT_GRANDE) return true;

            return false;
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
