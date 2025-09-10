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
    public partial class frmPrincipal : Form
    {
        private Form _formHijoActual; // referencia al formulario cargado en pnlCentral
        public frmPrincipal()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;


            // engancha eventos (si no lo hiciste en el diseñador)
            this.btnImpresion.Click += btnImpresion_Click;
            this.btnCodigos.Click += btnCodigos_Click;
        }
        private void MostrarEnCentral(Form formHijo)
        {
            // Cierra/limpia lo anterior
            if (_formHijoActual != null)
            {
                _formHijoActual.Close();
                _formHijoActual.Dispose();
                _formHijoActual = null;
            }

            foreach (Control c in pnlCentral.Controls) c.Dispose();
            pnlCentral.Controls.Clear();

            // Embebe el nuevo form
            _formHijoActual = formHijo;
            formHijo.TopLevel = false;
            formHijo.FormBorderStyle = FormBorderStyle.None;
            formHijo.Dock = DockStyle.Fill;

            pnlCentral.Controls.Add(formHijo);
            formHijo.Show();
            formHijo.BringToFront();
        }
        private void frmPrincipal_Load(object sender, EventArgs e)
        {
            StartPosition = FormStartPosition.CenterParent;
        }

        private void btnImpresion_Click(object sender, EventArgs e)
        {
            // ajusta el nombre de la clase al tuyo real
            var frm = new CapaPresentacion.Administrador.frmImpresoras();
            MostrarEnCentral(frm);
        }

        private void btnCodigos_Click(object sender, EventArgs e)
        {
            // ajusta el nombre de la clase al tuyo real
            var frm = new CapaPresentacion.Administrador.frmCodigosUsuarios();
            MostrarEnCentral(frm);
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnDespl_Click(object sender, EventArgs e)
        {

        }
    }
}
