using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaNegocio;

namespace CapaPresentacion.Administrador
{
    public partial class frmListImpresoras : Form
    {
        private readonly cnImpresora _svc = new cnImpresora();

        public string CodigoSeleccionado { get; private set; } // "001"
        public string NombreSeleccionado { get; private set; } // "PASTELERIA - HELADERIA"

        public frmListImpresoras()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            AcceptButton = btnAceptar;
            CancelButton = btnClose; // si no tienes este botón, quítalo

            ConfigurarCombo();

            Load += frmListImpresoras_Load;
            btnAceptar.Click += btnAceptar_Click;
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        private void frmListImpresoras_Load(object sender, EventArgs e)
        {
          //  CargarImpresorasWindows();
            // Trae catálogo M_FRMIMP: CDG_FORM, DES_FORM
            DataTable dt = _svc.ListarFormasImpresora();
            cboImpresoras.DataSource = dt;
            cboImpresoras.ValueMember = "CDG_FORM"; // código 3 dígitos
            cboImpresoras.DisplayMember = "DES_FORM"; // nombre legible
            cboImpresoras.SelectedIndex = dt.Rows.Count > 0 ? 0 : -1;
        }
        private void ConfigurarCombo()
        {
            cboImpresoras.AutoCompleteMode = AutoCompleteMode.None;
            cboImpresoras.DropDownStyle = ComboBoxStyle.DropDownList;
            cboImpresoras.AutoCompleteSource = AutoCompleteSource.ListItems;
            cboImpresoras.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
        }

        private void CargarImpresorasWindows()
        {
            try
            {
                cboImpresoras.BeginUpdate();
                cboImpresoras.Items.Clear();

                // Impresoras instaladas de Windows (solo nombre), ordenadas
                foreach (string nombre in PrinterSettings.InstalledPrinters.Cast<string>().OrderBy(n => n))
                    cboImpresoras.Items.Add(nombre);

                // Selecciona la predeterminada si está en la lista
                string predeterminada = new PrinterSettings().PrinterName;
                int idx = cboImpresoras.FindStringExact(predeterminada);
                cboImpresoras.SelectedIndex = idx >= 0 ? idx : (cboImpresoras.Items.Count > 0 ? 0 : -1);
            }
            finally
            {
                cboImpresoras.EndUpdate();
            }
        }

        private void btnAceptar_Click(object sender, EventArgs e)
        {
            if (cboImpresoras.SelectedValue == null)
            {
                MessageBox.Show("Selecciona una impresora/formato.");
                return;
            }

            CodigoSeleccionado = cboImpresoras.SelectedValue.ToString(); // "001"
            NombreSeleccionado = cboImpresoras.Text;                     // "PASTELERIA - HELADERIA"
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
