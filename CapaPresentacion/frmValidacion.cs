using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            this.AcceptButton = btnAceptar; // Enter confirma
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {

            string codigo = txtVendedor.Text.Trim();
            if (string.IsNullOrWhiteSpace(codigo))
            {
                MessageBox.Show("Ingrese el código de usuario.", "Validación",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtVendedor.Focus();
                return;
            }

            var vend = _svc.Validar(codigo, soloActivos: true); // tu cnVendedor.Validar(...)
            if (vend != null)
            {
                SesionActual.Vendedor = vend; // ← guarda vendedor

                MessageBox.Show($"Ingreso exitoso. Bienvenido {vend.Nombre}.",
                                "Validación", MessageBoxButtons.OK, MessageBoxIcon.Information);

                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Datos incorrectos o vendedor inactivo.",
                                "Validación", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtVendedor.SelectAll();
                txtVendedor.Focus();
            }
        }
    }
}
