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
    public partial class frmNSandwich : Form
    {
        public string TextoInicial { get; set; } = string.Empty;
        public string ProductoBaseTexto { get; set; } = string.Empty;
        public string Notas { get; private set; } = string.Empty;

        private TextBoxBase _txtNotas;
        private Control _txtNotasCtrl;

        // NUEVO (opcional, por si luego quieres evitar doble cableo)
        private readonly HashSet<Control> _chipsWired = new HashSet<Control>();

        public frmNSandwich()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            Load += frmNSandwich_Load;
        }
        private void frmNSandwich_Load(object sender, EventArgs e)
        {
            // 1) Encabezado
            var txtProd = this.Controls.Find("txtProductoSelect", true).FirstOrDefault();
            if (txtProd != null && !string.IsNullOrWhiteSpace(ProductoBaseTexto))
                TrySetText(txtProd, ProductoBaseTexto);

            // 2) Resolver área de notas (TextBoxBase o, p.ej., Guna2TextBox)
            _txtNotas = this.Controls.Find("txtNotasSandwich", true).OfType<TextBoxBase>().FirstOrDefault();
            if (_txtNotas == null)
                _txtNotasCtrl = this.Controls.Find("txtNotasSandwich", true).FirstOrDefault();

            // 3) Botones principales (resolver por Control; Guna2Button no es System.Button)
            var btnContinuar = this.Controls.Find("btnContinuar", true).FirstOrDefault();
            var btnEliminar = this.Controls.Find("btnEliminar", true).FirstOrDefault();
            var btnCerrar = this.Controls.Find("btnCerrar", true).FirstOrDefault();

            // 3.1 Continuar
            if (btnContinuar != null)
            {
                btnContinuar.Click -= btnContinuar_Click;
                btnContinuar.Click += btnContinuar_Click;

                if (btnContinuar is IButtonControl ib)
                    this.AcceptButton = ib; // ENTER => Continuar
            }

            //// 3.2 Eliminar: oculto pero acción disponible por tecla
            //if (btnEliminar != null)
            //{
            //    btnEliminar.Visible = false;   // << no mostrar
            //    btnEliminar.TabStop = false;
            //    btnEliminar.Click -= btnEliminar_Click;
            //    btnEliminar.Click += btnEliminar_Click; // por si quieres invocarlo manualmente
            //}

            //// 3.3 Cerrar: oculto, acción por ESC
            //if (btnCerrar != null)
            //{
            //    btnCerrar.Visible = false;   // << no mostrar
            //    btnCerrar.TabStop = false;
            //    // Si tienes un btnCerrar_Click, puedes mantenerlo, pero lo haremos con ESC.
            //}

            // 4) Precarga de notas (si llegó algo)
            if (!string.IsNullOrEmpty(TextoInicial))
                TrySetNotasText(TextoInicial);

            // 5) Enganchar TODOS los “chips” (excepto continuar/eliminar/cerrar y el área de notas)
            WireChipButtons(this, btnContinuar, btnEliminar, btnCerrar);
        }
        private void WireChipButtons(Control root, Control btnContinuar, Control btnEliminar, Control btnCerrar)
        {
            if (root == null) return;

            foreach (Control c in root.Controls)
            {
                WireChipButtons(c, btnContinuar, btnEliminar, btnCerrar);

                // Nunca el área de notas
                if (ReferenceEquals(c, _txtNotas) || ReferenceEquals(c, _txtNotasCtrl)) continue;
                if (c is TextBoxBase) continue;
                var tn = c.GetType().Name ?? "";
                if (tn.IndexOf("TextBox", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                // ¿Es botón chip?
                bool esBtn = (c is Button) ||
                             (tn.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0) ||
                             ((c.Name ?? "").StartsWith("btn", StringComparison.OrdinalIgnoreCase));

                if (!esBtn) continue;

                // Excluir acciones
                string name = (c.Name ?? "").ToLowerInvariant();
                if (ReferenceEquals(c, btnContinuar) || ReferenceEquals(c, btnEliminar) || ReferenceEquals(c, btnCerrar) ||
                    name == "btncontinuar" || name == "btneliminar" || name == "btncerrar")
                    continue;

                if (_chipsWired.Add(c))
                {
                    c.Click -= Chip_Click;
                    c.Click += Chip_Click;
                }
            }
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
        private static void TrySetText(Control any, string text)
        {
            try { any?.GetType().GetProperty("Text")?.SetValue(any, text, null); }
            catch { }
        }
        private void TrySetNotasText(string s)
        {
            if (s == null) s = string.Empty;   // ← en lugar de: s ??= string.Empty;

            if (_txtNotas != null)
            {
                _txtNotas.Text = s;
                _txtNotas.SelectionStart = _txtNotas.TextLength;
                _txtNotas.SelectionLength = 0;
                _txtNotas.ScrollToCaret();
                _txtNotas.Focus();
            }
            else if (_txtNotasCtrl != null)
            {
                _txtNotasCtrl.Text = s;

                // Ubica el caret al final si es Guna2TextBox u otro control con TextBox interno
                try
                {
                    var pSelStart = _txtNotasCtrl.GetType().GetProperty("SelectionStart");
                    var pSelLen = _txtNotasCtrl.GetType().GetProperty("SelectionLength");
                    var pText = _txtNotasCtrl.GetType().GetProperty("Text");

                    if (pSelStart != null && pSelLen != null && pText != null)
                    {
                        int len = ((string)pText.GetValue(_txtNotasCtrl, null))?.Length ?? 0;
                        pSelStart.SetValue(_txtNotasCtrl, len, null);
                        pSelLen.SetValue(_txtNotasCtrl, 0, null);
                    }
                    else
                    {
                        var innerProp = _txtNotasCtrl.GetType().GetProperty(
                            "TextBox",
                            System.Reflection.BindingFlags.Instance |
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.NonPublic);

                        var inner = innerProp?.GetValue(_txtNotasCtrl, null) as TextBoxBase;
                        if (inner != null)
                        {
                            inner.SelectionStart = inner.TextLength;
                            inner.SelectionLength = 0;
                            inner.ScrollToCaret();
                        }
                    }
                }
                catch { /* ignore */ }

                _txtNotasCtrl.Focus();
            }
        }
        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
