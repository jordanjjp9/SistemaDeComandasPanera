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
    public partial class frmMesas : Form
    {
        private Form _formActual;

        public frmMesas()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            pnlTop.SizeChanged += pnlTop_SizeChanged;
        }

        /*
        private void AbrirEnPanel<T>() where T : Form, new()
        {
            // Si ya está abierto ese mismo formulario, no vuelvas a cargarlo
            if (_formActual is T) return;

            // Cierra/dispone el anterior (si lo hubiera)
            if (_formActual != null)
            {
                _formActual.Close();
                _formActual.Dispose();
                _formActual = null;
            }

            // Crea y configura el nuevo
            var frm = new T
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill,
                StartPosition = FormStartPosition.Manual
            };

            pnlMCentral.SuspendLayout();
            pnlMCentral.Controls.Clear();
            pnlMCentral.Controls.Add(frm);
            frm.Show();
            pnlMCentral.ResumeLayout();

            _formActual = frm;
        }*/

        private void CentrarEnPanel(Control hijo)
        {
            // importante: sin Dock ni Anchor para que no “se pegue” a los bordes
            hijo.Anchor = AnchorStyles.None;

            int x = Math.Max(0, (pnlMCentral.ClientSize.Width - hijo.Width) / 2);
            int y = Math.Max(0, (pnlMCentral.ClientSize.Height - hijo.Height) / 2);
            hijo.Location = new Point(x, y);
        }

        private void AbrirEnPanel<T>(bool centrar = false) where T : Form, new()
        {
            // Si ya está abierto ese mismo formulario, no vuelvas a cargarlo
            if (_formActual is T)
            {
                // si ya está y pediste centrar, re-centramos por si cambió el tamaño del panel
                if (centrar && _formActual != null && _formActual.Dock == DockStyle.None)
                    CentrarEnPanel(_formActual);
                return;
            }

            // Quitar handler anterior de SizeChanged si lo hubiera (lo guardamos en Tag)
            if (pnlMCentral.Tag is EventHandler oldHandler)
                pnlMCentral.SizeChanged -= oldHandler;

            // Cierra/dispone el anterior
            if (_formActual != null)
            {
                _formActual.Close();
                _formActual.Dispose();
                _formActual = null;
            }

            // Crea y configura el nuevo
            var frm = new T
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual
            };

            // Dock según el modo
            if (centrar)
                frm.Dock = DockStyle.None;  // necesario para poder centrar
            else
                frm.Dock = DockStyle.Fill;  // comportamiento original

            pnlMCentral.SuspendLayout();
            pnlMCentral.Controls.Clear();
            pnlMCentral.Controls.Add(frm);
            frm.Show(); // asegura Handle

            if (centrar)
            {
                // Centrar ahora…
                CentrarEnPanel(frm);

                // …y mantenerlo centrado cuando cambie el tamaño del panel
                EventHandler handler = (_, __) => CentrarEnPanel(frm);
                pnlMCentral.SizeChanged += handler;
                pnlMCentral.Tag = handler; // guardamos para removerlo la próxima vez
            }

            pnlMCentral.ResumeLayout();
            _formActual = frm;
        }

        // Método público para mostrar validación
        public void MostrarValidacion()
        {
            using (var val = new frmValidacion())
            {
                if (val.ShowDialog(this) == DialogResult.OK)
                {
                    AbrirMenuPrincipal();
                }
            }
        }

        // Método público para abrir el menú principal de forma modal
        public void AbrirMenuPrincipal()
        {

            using (var val = new frmMenuPrincipal())
            {
                val.StartPosition = FormStartPosition.CenterParent;
                val.ShowDialog(this);
            }
        }

    //    private void btnSalon_Click(object sender, EventArgs e) => AbrirEnPanel<CapaPresentacion.Ambientes.frmSPrincipal>(centrar: true);

        private void btnDelivery_Click(object sender, EventArgs e)  => AbrirEnPanel<frmDelivery>();

        private void btnRappi_Click(object sender, EventArgs e)=> AbrirEnPanel<frmARappi>();

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmMesas_Load(object sender, EventArgs e)
        {
            pnlMCentral.Controls.Clear();

            var salon = new CapaPresentacion.Ambientes.frmSPrincipal
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual   // CenterParent NO aplica a TopLevel=false
                                                           // NO pongas Dock = Fill si lo quieres centrado
            };

            pnlMCentral.Controls.Add(salon);
            salon.Show();

            // Centrar ahora…
            CentrarEnPanel(salon, pnlMCentral);

            // …y cada vez que cambie el tamaño del panel
            pnlMCentral.SizeChanged += (_, __) => CentrarEnPanel(salon, pnlMCentral);
        }


        private void CentrarEnPanel(Form hijo, Control contenedor)
        {
            // Si el hijo es más grande que el contenedor, lo pegamos en (0,0)
            int x = Math.Max(0, (contenedor.ClientSize.Width - hijo.Width) / 2);
            int y = Math.Max(0, (contenedor.ClientSize.Height - hijo.Height) / 2);
            hijo.Location = new Point(x, y);
        }

        private void pnlTop_SizeChanged(object sender, EventArgs e)
        {
            pnlButons.Left = (pnlTop.Width - pnlButons.Width) / 2;
            pnlButons.Top = (pnlTop.Height - pnlButons.Height) / 2;
        }

        private void btnSalon_Click(object sender, EventArgs e) => AbrirEnPanel<CapaPresentacion.Ambientes.frmSPrincipal>(centrar: true);

        //   private void btnSalon_Click_1(object sender, EventArgs e) => AbrirEnPanel<CapaPresentacion.Ambientes.frmSPrincipal>(centrar: true);
    }
}
