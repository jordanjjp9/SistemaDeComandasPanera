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
    public partial class frmLogin : Form
    {
        private const string USERNAME = "VENTAS";
        private const string PASSWORD = "1234";

        public frmLogin()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            txtPassword.UseSystemPasswordChar = true;
            this.AcceptButton = btnIngresar;
        }

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            string user = txtUser.Text.Trim();
            string pass = txtPassword.Text;

            // Username sin sensibilidad a mayúsculas, password exacta
            bool ok = (user == USERNAME) && (pass == PASSWORD);

            if (ok)
            {
                MessageBox.Show("Ingreso exitoso", "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Abrir frmMesas y cerrar el login sin terminar la app
                var frm = new frmMesas();
                frm.Show();
                this.Hide();
                // Cuando se cierre frmMesas, cerramos definitivamente el login
                frm.FormClosed += (s, args) => this.Close();
            }
            else
            {
                MessageBox.Show("Datos incorrectos. Inténtelo nuevamente.",
                                "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }

        private void lblTitle_SizeChanged(object sender, EventArgs e)
        {

        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
