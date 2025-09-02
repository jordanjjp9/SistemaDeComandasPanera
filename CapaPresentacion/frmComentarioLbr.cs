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

            // Enter enviará el formulario
            this.AcceptButton = btnEnviar;

            // Botones rápidos
            btnLlevar.Click += BotonRapido_Click;
            btnAviso.Click += BotonRapido_Click;

            // Ajustes para Guna2TextBox como multilínea
            this.Load += (s, e) =>
            {
                // Asegura multilínea (Guna2TextBox soporta estas propiedades)
                txtComentLibr.Multiline = true;
                txtComentLibr.AcceptsReturn = true;
                // txtComentLibr.WordWrap = true;  // si lo tienes disponible en tu versión

                // Precarga (solo si no hay texto ya asignado por 'Texto')
                if (string.IsNullOrEmpty(txtComentLibr.Text) && !string.IsNullOrEmpty(TextoInicial))
                {
                    Texto = TextoInicial; // reutilizamos normalización de saltos de línea
                }

                // Coloca el cursor al final
                txtComentLibr.SelectionStart = txtComentLibr.TextLength;
                txtComentLibr.ScrollToCaret();
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
