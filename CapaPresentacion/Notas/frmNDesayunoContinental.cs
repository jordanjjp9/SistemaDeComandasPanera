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
    public partial class frmNDesayunoContinental : Form
    {
        public string ProductoBaseTexto { get; set; } = string.Empty;
        public string Notas { get; private set; } = string.Empty;

        private TextBoxBase _txtNotas;
        private Control _txtNotasCtrl;

        public frmNDesayunoContinental()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            Load += frmNDesayunoContinental_Load;
        }

        private void frmNDesayunoContinental_Load(object sender, EventArgs e)
        {
            // encabezado
            var txtProd = this.Controls.Find("txtProductoSelect", true).FirstOrDefault();
            if (txtProd != null && !string.IsNullOrWhiteSpace(ProductoBaseTexto))
                txtProd.Text = ProductoBaseTexto;

            // resolver área de notas (TextBox o Guna2TextBox)
            _txtNotas = this.Controls.Find("txtNotasContinental", true).OfType<TextBoxBase>().FirstOrDefault();
            if (_txtNotas == null)
                _txtNotasCtrl = this.Controls.Find("txtNotasContinental", true).FirstOrDefault();

            // botones principales
            var btnContinuar = this.Controls.Find("btnContinuar", true).OfType<Button>().FirstOrDefault();
            var btnEliminar = this.Controls.Find("btnEliminar", true).OfType<Button>().FirstOrDefault();

            if (btnContinuar != null) { btnContinuar.Click -= btnContinuar_Click; btnContinuar.Click += btnContinuar_Click; AcceptButton = btnContinuar; }
            if (btnEliminar != null) { btnEliminar.Click -= btnEliminar_Click; btnEliminar.Click += btnEliminar_Click; }

            // enganchar todos los “chips” (excepto continuar/eliminar)
            WireChipButtons(this, btnContinuar, btnEliminar);
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

        private void btnCerrar_Click(object sender, EventArgs e)
        {

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
    }
}
