using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaEntidad;
using CapaPresentacion.Helpers;

namespace CapaPresentacion.Ambientes
{
    public partial class frmSPrincipal : Form
    {
        public frmSPrincipal()
        {
            InitializeComponent();
            this.Load += Frm_Load;
            this.StartPosition = FormStartPosition.CenterScreen;

            WireMesaButtons(this);

            // Si en el futuro agregas mesas dinámicamente:
            this.ControlAdded += (_, ev) => WireMesaButtons(ev.Control);

        }

        private void Frm_Load(object sender, EventArgs e)
        {
            // Busca y enlaza TODOS los botones que se llamen btnMesa*
            // Si tus mesas están dentro de un panel específico, cámbialo por ese panel:
            // WireMesaButtons(pnlMesas);
            WireMesaButtons(this);
        }

       /* private void frmSPrincipal_Load(object sender, EventArgs e)
        {

        }*/

        private void WireMesaButtons(Control root)
        {
            // ¿Este control es una “mesa”?
            if (root != null && root.Name.StartsWith("btnMesa", StringComparison.OrdinalIgnoreCase))
            {
                root.Click -= BtnMesa_Click;
                root.Click += BtnMesa_Click;
            }

            // Recorre hijos recursivamente
            foreach (Control c in root.Controls)
                WireMesaButtons(c);
        }

        private void BtnMesa_Click(object sender, EventArgs e)
        {
            var ctrl = sender as Control;               // sirve para Button y Guna2Button
            string name = ctrl?.Name ?? "";
            string text = ctrl?.Text ?? "";

            int num = ExtraerNumero(name);
            if (num == 0) num = ExtraerNumero(text);

            // Guarda selección
            SesionActual.Ambiente = "SALON";
            SesionActual.Mesa = new ceMesa { Numero = num };

            // pídelo al host (frmMesas). FindForm funciona aunque este form esté embebido.
            var host = this.FindForm() as frmMesas
                    ?? this.TopLevelControl as frmMesas
                    ?? Application.OpenForms.OfType<frmMesas>().FirstOrDefault();

            host?.MostrarValidacion();
        }

        private static int ExtraerNumero(string s)
        {
            var m = Regex.Match(s ?? "", @"\d+");
            return m.Success ? int.Parse(m.Value) : 0;
        }
    }
}
