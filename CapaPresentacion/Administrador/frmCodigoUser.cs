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
        public frmCodigoUser()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterParent;

            this.AcceptButton = btnAceptar;
            this.CancelButton = btnCerrar;

            txtIngCod.MaxLength = 4;
            txtIngCod.KeyPress += txtIngCod_KeyPress;
        }

        private void txtIngCod_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            string v = (txtIngCod.Text ?? "").Trim();

            if (v.Length != 4 || !v.All(char.IsDigit))
            {
                MessageBox.Show("Debe ingresar exactamente 4 dígitos numéricos.", "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtIngCod.Focus();
                txtIngCod.SelectAll();
                return;
            }

            CodigoIngresado = v;
            this.DialogResult = DialogResult.OK;
            this.Close();
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
    }
}
