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

namespace CapaPresentacion
{
    public partial class frmSalonPrincipal : Form
    {
        public frmSalonPrincipal()
        {
            InitializeComponent();
            this.Load += Frm_Load;
        }

        private void Frm_Load(object sender, EventArgs e)
        {
            // Busca y enlaza TODOS los botones que se llamen btnMesa*
            // Si tus mesas están dentro de un panel específico, cámbialo por ese panel:
            // WireMesaButtons(pnlMesas);
            WireMesaButtons(this);
        }

        private void WireMesaButtons(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is Button btn && btn.Name.StartsWith("btnMesa", StringComparison.OrdinalIgnoreCase))
                {
                    btn.Click -= BtnMesa_Click; // evita doble suscripción si recargas
                    btn.Click += BtnMesa_Click;
                }
                // Recursivo: por si los botones están dentro de paneles
                if (c.HasChildren)
                    WireMesaButtons(c);
            }
        }

        private void BtnMesa_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;

            int num = ExtraerNumero(btn.Name);
            if (num == 0) num = ExtraerNumero(btn.Text);

            // Guarda selección
            SesionActual.Ambiente = "SALON";                // "DELIVERY" o "RAPPI" en los otros forms
            SesionActual.Mesa = new ceMesa { Numero = num };

            // Pide validación al host
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
