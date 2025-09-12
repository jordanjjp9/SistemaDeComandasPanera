using System;
using System.Data;
using System.Windows.Forms;
using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Helpers;

namespace CapaPresentacion
{
    public partial class frmValidacion : Form
    {
        private readonly cnVendedor _svc = new cnVendedor();

        public frmValidacion()
        {
            InitializeComponent();

            // UX básica
            this.AcceptButton = btnAceptar;                 // Enter confirma
            this.StartPosition = FormStartPosition.CenterScreen;
            this.KeyPreview = true;                         // para capturar ESC si quieres cerrar
            this.Shown += frmValidacion_Shown;
            this.KeyDown += frmValidacion_KeyDown;
        }

        private void frmValidacion_Shown(object sender, EventArgs e)
        {
            // Foco inicial en el cuadro de usuario
            if (txtVendedor != null)
            {
                txtVendedor.Focus();
                txtVendedor.SelectAll();
            }
        }

        private void frmValidacion_KeyDown(object sender, KeyEventArgs e)
        {
            // Cerrar con ESC (opcional)
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            // Ahora el textbox representa el USUARIO (CDG_USR)
            string usr = txtVendedor?.Text?.Trim();

            if (string.IsNullOrWhiteSpace(usr))
            {
                MessageBox.Show("Ingrese el usuario (CDG_USR).", "Validación",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtVendedor?.Focus();
                return;
            }

            // Login por USR (sin PIN porque tu tabla M_VENDED no lo tiene)
            var res = _svc.LoginPorUsr(usr, soloActivos: true);

            if (!res.Ok)
            {
                MessageBox.Show(res.Motivo ?? "No autorizado.",
                                "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (txtVendedor != null)
                {
                    txtVendedor.SelectAll();
                    txtVendedor.Focus();
                }
                return;
            }

            // Compatibilidad: guardamos el vendedor en la sesión como siempre
            //   res.Vendedor.Codigo -> CDG_VEND (para M_PEDIDO)
            //   res.Vendedor.CdgUsr -> CDG_USR (por si lo usas en la UI / auditoría)
            SesionActual.Vendedor = res.Vendedor;

            MessageBox.Show($"Ingreso exitoso. Bienvenido {res.Vendedor.Nombre}.",
                            "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
