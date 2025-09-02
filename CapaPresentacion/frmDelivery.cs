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
    public partial class frmDelivery : Form
    {
        public frmDelivery()
        {
            InitializeComponent();
            this.Load += frmDelivery_Load;
        }

        private void frmDelivery_Load(object sender, EventArgs e)
        {
            foreach (var btn in GetMesaButtons())
            {
                btn.Click -= BtnMesa_Click;  // evita doble suscripción si recargas
                btn.Click += BtnMesa_Click;
            }
        }

        private Button[] GetMesaButtons()
        {
            // Si tus botones tienen otro prefijo (p. ej. btnDelMesa), cambia StartsWith("btnMesa")
            return GetAllControls(this)
                   .OfType<Button>()
                   .Where(b => b.Name.StartsWith("btnMesa")) // ej.: btnMesa980, btnMesa981...
                   .ToArray();
        }

        private static System.Collections.Generic.IEnumerable<Control> GetAllControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var child in GetAllControls(c))
                    yield return child;
            }
        }

        private void BtnMesa_Click(object sender, EventArgs e)
        {
            var btn = (Button)sender;

            int num = ExtraerNumero(btn.Name);
            if (num == 0) num = ExtraerNumero(btn.Text);

            // Guarda selección
            SesionActual.Ambiente = "DELIVERY";                // "DELIVERY" o "RAPPI" en los otros forms
            SesionActual.Mesa = new ceMesa { Numero = num };

            // Pide validación al host
            var host = this.FindForm() as frmMesas
                     ?? this.TopLevelControl as frmMesas
                     ?? Application.OpenForms.OfType<frmMesas>().FirstOrDefault();

            host?.MostrarValidacion();
        }

        // frmValidacion modal: bloquea SOLO este formulario mientras valida
        private bool ValidarAcceso()
        {
            using (var dlg = new frmValidacion())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                return dlg.ShowDialog(this) == DialogResult.OK;
            }
        }

        private void AbrirMenuPrincipalConOwner(int mesaSeleccionada)
        {
            // Si Delivery está cargado dentro de frmMesas, FindForm() devuelve ese frmMesas
            var frmMesas = this.FindForm() as frmMesas;

            var menu = new frmMenuPrincipal
            {
                StartPosition = FormStartPosition.CenterScreen
            };

            // (Opcional) pasar la mesa seleccionada
            // menu.MesaSeleccionada = mesaSeleccionada;

            if (frmMesas != null)
                menu.Show(frmMesas);  // owned form → siempre por encima de frmMesas
            else
                menu.Show();
        }

        // Utilidad: extraer número (btnMesa980 -> 980, "MESA 980" -> 980)
        private int ExtraerNumero(string s)
        {
            var m = Regex.Match(s ?? "", @"\d+");
            return m.Success ? int.Parse(m.Value) : 0;
        }

    }
}
