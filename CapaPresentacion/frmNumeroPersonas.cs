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
    public partial class frmNumeroPersonas : Form
    {
        public int Cantidad { get; private set; } = 1;

        public frmNumeroPersonas()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton = btnAceptar;

            Load += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(txtCantPers.Text)) txtCantPers.Text = "1";
                txtCantPers.SelectAll();
                txtCantPers.Focus();
            };

            // Solo dígitos
            txtCantPers.KeyPress += (s, e) =>
            {
                if (char.IsControl(e.KeyChar)) return;
                if (!char.IsDigit(e.KeyChar)) e.Handled = true;
            };

            btnAceptar.Click += btnAceptar_Click;
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            if (!int.TryParse(txtCantPers.Text.Trim(), out var n) || n <= 0)
            {
                MessageBox.Show("Ingresa un número válido de personas.", "Validación",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCantPers.SelectAll();
                txtCantPers.Focus();
                return;
            }

            Cantidad = n;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
