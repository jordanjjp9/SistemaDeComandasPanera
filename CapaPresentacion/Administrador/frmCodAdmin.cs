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
    public partial class frmCodAdmin : Form
    {
        public frmCodAdmin()
        {
            InitializeComponent();

            // Teclas globales
            this.KeyPreview = true;
            this.KeyDown += frmCodAdmin_KeyDown;

            // Aceptar con Enter
            this.AcceptButton = btnAceptar;

            // Opcional: enmascarar como contraseña
            txtCodAdm.UseSystemPasswordChar = true;

            // Eventos
            this.Shown += frmCodAdmin_Shown;
            btnAceptar.Click -= btnAceptar_Click;
            btnAceptar.Click += btnAceptar_Click;
        }
        public string CodigoIngresado
        {
            get
            {
                // ajusta el nombre del TextBox si es distinto
                return (txtCodAdm?.Text ?? string.Empty).Trim();
            }
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void frmCodAdmin_Shown(object sender, EventArgs e)
        {
            txtCodAdm.Focus();
            txtCodAdm.SelectAll();
        }

        private void frmCodAdmin_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }
    }
}
