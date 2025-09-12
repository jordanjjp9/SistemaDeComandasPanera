using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion.Administrador
{
    public partial class frmCodigoUser : Form
    {
        public string CodigoIngresado { get; private set; }

        /// <summary>
        /// (Opcional) Valor inicial que quieres mostrar en el textbox al abrir el diálogo.
        /// </summary>
        public string CodigoInicial { get; set; }
        public frmCodigoUser()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;


            // UX / comportamiento
            StartPosition = FormStartPosition.CenterParent;
            AcceptButton = btnAceptar;
            CancelButton = btnCerrar;

            // TextBox: solo números, máximo 4
            txtIngCod.MaxLength = 4;
            txtIngCod.KeyPress += txtIngCod_KeyPress;

            // Al mostrar, poner foco y precargar si hay valor inicial
            Shown += frmCodigoUser_Shown;
        }

        private void txtIngCod_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir teclas de control (Backspace, etc.) y solo dígitos
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            string v = (txtIngCod.Text ?? string.Empty).Trim();

            // Validación estricta: exactamente 4 dígitos numéricos
            if (v.Length != 4 || !v.All(char.IsDigit))
            {
                MessageBox.Show(
                    "Debe ingresar exactamente 4 dígitos numéricos.",
                    "Validación",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                txtIngCod.Focus();
                txtIngCod.SelectAll();
                return;
            }

            CodigoIngresado = v;
            DialogResult = DialogResult.OK;
            Close();
        }
        private static bool EsNumerico(string s)
        {
            foreach (char c in s) if (!char.IsDigit(c)) return false;
            return true;
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void frmCodigoUser_Shown(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CodigoInicial))
                txtIngCod.Text = CodigoInicial.Trim();

            txtIngCod.Focus();
            txtIngCod.SelectAll();
        }
    }
}
