using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion
{
    public partial class frmComentarioLbr : Form
    {
        // Texto final que el host (frmMenuPrincipal) leerá si el usuario pulsa "Enviar"
        public string Comentario { get; private set; } = string.Empty;

        // Opción 1 (recomendada): el host puede asignar aquí para precargar el diálogo.
        // Se aplicará en el Load si el TextBox está vacío.
        public string TextoInicial { get; set; } = string.Empty;

        // Opción 2 (compatible con tu código actual): set/get directo del contenido.
        // Si el host usa esta propiedad antes de ShowDialog, el contenido aparece de inmediato.
        public string Texto
        {
            get => txtComentLibr.Text;
            set
            {
                txtComentLibr.Text = (value ?? string.Empty)
                    .Replace("\r\n", "\n")
                    .Replace("\n", Environment.NewLine);

                txtComentLibr.SelectionStart = txtComentLibr.TextLength;
                txtComentLibr.ScrollToCaret();
            }
        }

        public frmComentarioLbr()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            AcceptButton = btnEnviar;         // Enter enviará cuando el foco no esté en el textbox

            // Enfocar el textbox cuando el form ya está visible
            this.Shown += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    if (txtComentLibr.CanFocus)
                    {
                        this.ActiveControl = txtComentLibr;
                        txtComentLibr.Focus();
                        txtComentLibr.SelectionStart = txtComentLibr.TextLength;
                        txtComentLibr.SelectionLength = 0;
                    }
                }));
            };

            // Enter = Enviar | Shift+Enter = salto de línea
            txtComentLibr.Multiline = true;
            txtComentLibr.AcceptsReturn = true; // mantiene saltos con Shift+Enter
            txtComentLibr.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && !e.Shift && !e.Control)
                {
                    e.SuppressKeyPress = true;   // no agregues el salto
                    btnEnviar.PerformClick();    // dispara el envío
                }
                // con Shift+Enter seguirá agregando una nueva línea
            };
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnEnviar_Click(object sender, EventArgs e)
        {
            // Devuelve el contenido tal cual (preservando saltos)
            Comentario = txtComentLibr.Text;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void BotonRapido_Click(object sender, EventArgs e)
        {
            if (sender is Control b)
            {
                // Si el texto actual no termina en salto, agrega uno antes
                bool necesitaSaltoPrevio = txtComentLibr.TextLength > 0 &&
                                           !txtComentLibr.Text.EndsWith(Environment.NewLine);

                if (necesitaSaltoPrevio)
                    txtComentLibr.AppendText(Environment.NewLine);

                txtComentLibr.AppendText($"- {b.Text}{Environment.NewLine}");

                txtComentLibr.SelectionStart = txtComentLibr.TextLength; // cursor al final
                txtComentLibr.ScrollToCaret();
                txtComentLibr.Focus();
            }
        }
    }
}
