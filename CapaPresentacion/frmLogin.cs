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
        // Credenciales VENTAS
        private const string USER_VENTAS = "VENTAS";
        private const string PASS_VENTAS = "1234";

        // Credenciales ADMIN
        private const string USER_ADMIN = "ADMINISTRADOR";
        private const string PASS_ADMIN = "Root123$";

        public frmLogin()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            txtPassword.UseSystemPasswordChar = true;
            this.AcceptButton = btnIngresar;
            // en el constructor, después de InitializeComponent():
            this.Load += frmLogin_Load;

            //   txtUser.Focus();
        }

        private void btnIngresar_Click(object sender, EventArgs e)
        {
            string user = (txtUser.Text ?? string.Empty).Trim();
            string pass = txtPassword.Text ?? string.Empty;

            // Username sin sensibilidad a mayúsculas, password exacta
            bool esVentas = user.Equals(USER_VENTAS, StringComparison.OrdinalIgnoreCase) && pass == PASS_VENTAS;
            bool esAdmin = user.Equals(USER_ADMIN, StringComparison.OrdinalIgnoreCase) && pass == PASS_ADMIN;

            if (esVentas)
            {
                MessageBox.Show("Ingreso exitoso (VENTAS)", "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);

                var frm = new frmMesas();
                frm.Show();
                this.Hide();
                frm.FormClosed += (s, args) => this.Close();
                return;
            }

            if (esAdmin)
            {
                MessageBox.Show("Ingreso exitoso (ADMINISTRADOR)", "Login", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Abrir el formulario principal de Administrador
                var frm = new CapaPresentacion.Administrador.frmPrincipal();
                frm.StartPosition = FormStartPosition.CenterScreen;
                frm.Show();
                this.Hide();
                frm.FormClosed += (s, args) => this.Close();
                return;
            }

            // Credenciales inválidas
            MessageBox.Show("Datos incorrectos. Inténtelo nuevamente.",
                            "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            txtPassword.Clear();
            txtPassword.Focus();
        }

        private void lblTitle_SizeChanged(object sender, EventArgs e)
        {
            // (opcional) lógica de UI si la necesitas
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmLogin_Load(object sender, EventArgs e)
        {
            txtUser.Focus();
        }
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtUser.Focus();
         //   txtUser.SelectAll(); // si quieres seleccionar el texto existente
        }
    }
}
