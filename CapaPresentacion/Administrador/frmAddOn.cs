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
    public partial class frmAddOn : Form
    {
        public frmAddOn()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnComentarioLrb_Click(object sender, EventArgs e)
        {
            CapaPresentacion.ConfiguracionesAdd.frmComentarioDirigido frmComtDrj = new ConfiguracionesAdd.frmComentarioDirigido();
            frmComtDrj .ShowDialog();
            Close();
        }
    }
}
