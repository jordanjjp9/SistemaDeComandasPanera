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
    public partial class frmNBebidas : Form
    {
        // --- Entrada ---
        public string ProductoBaseTexto { get; set; } = string.Empty; // "2 x DESAYUNO AMERICANO"
        public string TextoInicial { get; set; } = string.Empty; // lo que viene de frmCJugoDesayuno

        // --- Salida ---
        public string Notas { get; private set; } = string.Empty;

        // Referencias a controles del diseñador (por nombre):
        private TextBoxBase _txtNotas;     // txtNotasBebida como TextBox/RichTextBox
        private Control _txtNotasCtrl;     // fallback si fuera un Guna2TextBox
        private Control _txtProductoSelect;

        public frmNBebidas()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            Load += frmNBebidas_Load;
        }

        private void frmNBebidas_Load(object sender, EventArgs e)
        {
            // 1) Localiza el control de notas (agrego tus nombres reales)
            _txtNotas = FindNotasTextBox();            // TextBoxBase si existe
            if (_txtNotas == null)
                _txtNotasCtrl = FindNotasControl();    // Fallback (Guna2TextBox u otro)

            // 2) Enlaza botones por nombre
            var btnContinuar = this.Controls.Find("btnContinuar", true).OfType<Button>().FirstOrDefault();
            var btnEliminar = this.Controls.Find("btnEliminar", true).OfType<Button>().FirstOrDefault();

            if (btnContinuar != null)
            {
                btnContinuar.Click -= btnContinuar_Click;
                btnContinuar.Click += btnContinuar_Click;
                this.AcceptButton = btnContinuar;
            }
            if (btnEliminar != null)
            {
                btnEliminar.Click -= btnEliminar_Click;
                btnEliminar.Click += btnEliminar_Click;
            }

            // 3) Muestra el producto base arriba si tienes un txtProductoSelect en el diseño
            var txtProd = this.Controls.Find("txtProductoSelect", true).FirstOrDefault();
            if (txtProd != null && !string.IsNullOrWhiteSpace(ProductoBaseTexto))
                txtProd.Text = ProductoBaseTexto;

            // 4) Precarga lo que venga del formulario anterior (las líneas de jugo)
            if (!string.IsNullOrEmpty(TextoInicial))
                TrySetNotasText(TextoInicial);

            // 5) Engancha TODOS los botones “chips”: Button y Guna2Button
            WireQuickNoteButtons(this, btnContinuar, btnEliminar);
        }

        // ============ Botones principales ============
        private void btnContinuar_Click(object sender, EventArgs e)
        {
            Notas = ReadNotasText();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            // elimina la última línea no vacía
            var lines = GetLines();
            if (lines == null || lines.Length == 0) return;

            int i = lines.Length - 1;
            while (i >= 0 && string.IsNullOrWhiteSpace(lines[i])) i--;

            var nuevas = (i <= 0) ? Array.Empty<string>() : lines.Take(i).ToArray();
            SetLines(nuevas);
        }

        // ============ “Chips” / botones rápidos ============
        private void WireQuickNoteButtons(Control root, Button btnContinuar, Button btnEliminar)
        {
            if (root == null) return;

            foreach (Control c in root.Controls)
            {
                // Recursivo
                WireQuickNoteButtons(c, btnContinuar, btnEliminar);

                // Engancha cualquier control “tipo botón”
                bool esChip =
                    (c is Button) ||
                    (c.GetType().Name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (c.Name ?? "").StartsWith("btn", StringComparison.OrdinalIgnoreCase);

                if (!esChip) continue;

                // Excluir los de acción
                if (ReferenceEquals(c, btnContinuar) || ReferenceEquals(c, btnEliminar)) continue;

                c.Click -= btnCerrar_Click;
                c.Click += btnCerrar_Click;
            }
        }

        private void QuickButton_Click(object sender, EventArgs e)
        {
            var b = sender as Button;
            if (b == null) return;

            string actual = ReadNotasText() ?? string.Empty;
            if (actual.Length > 0 && !actual.EndsWith(Environment.NewLine))
                actual += Environment.NewLine;

            actual += "- " + (b.Text ?? string.Empty).Trim();
            TrySetNotasText(actual);
        }

        // ============ Helpers de texto ============
        private string ReadNotasText()
        {
            if (_txtNotas != null) return _txtNotas.Text;
            if (_txtNotasCtrl != null) return _txtNotasCtrl.Text ?? string.Empty;
            return string.Empty;
        }

        private void TrySetNotasText(string s)
        {
            s = s ?? string.Empty;
            if (_txtNotas != null)
            {
                _txtNotas.Text = s;
                _txtNotas.SelectionStart = _txtNotas.TextLength;
                _txtNotas.ScrollToCaret();
                _txtNotas.Focus();
            }
            else if (_txtNotasCtrl != null)
            {
                _txtNotasCtrl.Text = s;
                _txtNotasCtrl.Focus();
            }
        }

        private string[] GetLines()
        {
            var t = ReadNotasText() ?? string.Empty;
            t = t.Replace("\r\n", "\n");
            return t.Split('\n');
        }

        private void SetLines(string[] lines)
        {
            string joined = string.Join(Environment.NewLine, lines ?? Array.Empty<string>());
            TrySetNotasText(joined);
        }

        private TextBoxBase FindNotasTextBox()
        {
            // Incluyo tus nombres reales
            var candidatos = new[] { "txtNotasBebida", "txtNotasBCalient", "txtNBebidas", "txtNotas", "txtNota", "txtComentarios", "txtComentLibr" };
            foreach (var name in candidatos)
            {
                var tb = this.Controls.Find(name, true).OfType<TextBoxBase>().FirstOrDefault();
                if (tb != null) return tb;
            }
            // Si no encuentra por nombre, toma el primer TextBoxBase multilínea
            var firstMulti = this.Controls.OfType<TextBoxBase>().FirstOrDefault(t => t.Multiline);
            return firstMulti;
        }

        private Control FindNotasControl()
        {
            // Fallback para Guna2TextBox u otros (por nombre)
            var candidatos = new[] { "txtNotasBebida", "txtNotasBCalient", "txtNBebidas", "txtNotas", "txtNota", "txtComentarios", "txtComentLibr" };
            foreach (var name in candidatos)
            {
                var c = this.Controls.Find(name, true).FirstOrDefault();
                if (c != null) return c;
            }
            // Último recurso: cualquier control con propiedad Text
            return this.Controls.Cast<Control>().FirstOrDefault(c => c.GetType().GetProperty("Text") != null);
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            var c = sender as Control;
            if (c == null) return;

            var texto = (c.Text ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(texto)) return;

            string actual = ReadNotasText() ?? string.Empty;

            if (actual.Length > 0 && !actual.EndsWith(Environment.NewLine))
                actual += Environment.NewLine;

            actual += "- " + texto + Environment.NewLine;
            TrySetNotasText(actual);
        }
    }
}
