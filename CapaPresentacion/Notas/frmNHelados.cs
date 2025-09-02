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

namespace CapaPresentacion.Notas
{
    public partial class frmNHelados : Form
    {
        // ENTRADAS (las setea el host)
        public int Cantidad { get; set; } = 1;
        public string Producto { get; set; } = string.Empty;

        // SALIDAS (las lee el host al cerrar con OK)
        public string Notas => txtNotasHelado.Text.Trim();
        public string ProductoSelectLine { get; private set; } = string.Empty;

        private readonly List<string> _notas = new List<string>();

        public frmNHelados()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;

            Load += frmNHelados_Load;
            btnContinuar.Click += btnContinuar_Click;
            btnEliminar.Click += btnEliminar_Click;
        }


        private void pnlCabecera_SizeChanged(object sender, EventArgs e)
        {
            lblTitle.Left = (pnlCabecera.Width - lblTitle.Width) / 2;
            lblTitle.Top = (pnlCabecera.Height - lblTitle.Height) / 2;

        }

        private void RedibujarNotas()
        {
            // Normaliza saltos de línea y vuelca TODO
            txtNotasHelado.Text = string.Join(Environment.NewLine, _notas);
            txtNotasHelado.SelectionStart = txtNotasHelado.TextLength;
            txtNotasHelado.ScrollToCaret();
        }

        private void WireComentarioButtons(Control root)
        {
            if (root == null) return;

            // Si este control es un botón marcado con Tag="comentario", suscribe
            if ((root.Tag as string)?.Equals("comentario", StringComparison.OrdinalIgnoreCase) == true)
            {
                root.Click -= BotonRapido_Click; // evita doble suscripción
                root.Click += BotonRapido_Click;
            }

            // Recurre hijos
            foreach (Control c in root.Controls)
                WireComentarioButtons(c);
        }

        private void AgregarNota(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return;

            if (txtNotasHelado.TextLength > 0 &&
                !txtNotasHelado.Text.EndsWith(Environment.NewLine))
                txtNotasHelado.AppendText(Environment.NewLine);

            txtNotasHelado.AppendText("- " + texto.Trim().ToUpperInvariant() + Environment.NewLine);

            txtNotasHelado.SelectionStart = txtNotasHelado.TextLength;
            txtNotasHelado.ScrollToCaret();
        }

        private void BotonRapido_Click(object sender, EventArgs e)
        {
            if (sender is Control b)
            {
                // añade una línea con formato "- (Texto)"
                if (txtNotasHelado.TextLength > 0 &&
                    !txtNotasHelado.Text.EndsWith(Environment.NewLine))
                    txtNotasHelado.AppendText(Environment.NewLine);

                txtNotasHelado.AppendText($"- ({b.Text}){Environment.NewLine}");
                txtNotasHelado.SelectionStart = txtNotasHelado.TextLength;
                txtNotasHelado.ScrollToCaret();
                txtNotasHelado.Focus();
            }
        }

        private void Nota_Click(object sender, EventArgs e)
        {
            ////if (ReferenceEquals(sender, btnContinuar) || ReferenceEquals(sender, btnEliminar)) return;

            ////if (sender is Control b)
            ////{
            ////    string texto = (b.Text ?? "").Trim();
            ////    if (texto.Length == 0) return;

            ////    // evita doble salto si ya hay contenido
            ////    if (txtNotasHelado.TextLength > 0 &&
            ////        !txtNotasHelado.Text.EndsWith(Environment.NewLine))
            ////    {
            ////        txtNotasHelado.AppendText(Environment.NewLine);
            ////    }

            ////    txtNotasHelado.AppendText($"- {texto.ToUpperInvariant()}{Environment.NewLine}");
            ////    txtNotasHelado.SelectionStart = txtNotasHelado.TextLength;
            ////    txtNotasHelado.ScrollToCaret();
            ////}
            if (ReferenceEquals(sender, btnContinuar) || ReferenceEquals(sender, btnEliminar)) return;
            if (sender is Control c) AgregarNota(c.Text);
        }

        private void WireNotaButtons(Control root)
        {
            //////if (root == null) return;

            //////foreach (Control c in root.Controls)
            //////{
            //////    // No enganchar los botones de control
            //////    if (ReferenceEquals(c, btnContinuar) || ReferenceEquals(c, btnEliminar))
            //////    {
            //////        // nada
            //////    }
            //////    else
            //////    {
            //////        bool esBoton =
            //////            c is Button ||
            //////            (c.GetType().FullName?.IndexOf("Guna2Button", StringComparison.OrdinalIgnoreCase) >= 0);

            //////        if (esBoton)
            //////        {
            //////            c.Click -= Nota_Click;
            //////            c.Click += Nota_Click;
            //////        }
            //////    }

            //////    if (c.HasChildren) WireNotaButtons(c);
            //////}
            ///
            if (root == null) return;

            foreach (Control c in root.Controls)
            {
                // Nunca estos
                if (ReferenceEquals(c, btnContinuar) || ReferenceEquals(c, btnEliminar))
                {
                    // seguir recorriendo hijos por si tienen botones dentro
                }
                else
                {
                    bool esGuna = c.GetType().Name.IndexOf("Guna2Button", StringComparison.OrdinalIgnoreCase) >= 0
                               || c.GetType().Name.IndexOf("Guna2ImageButton", StringComparison.OrdinalIgnoreCase) >= 0
                               || c.GetType().Name.IndexOf("Guna2GradientButton", StringComparison.OrdinalIgnoreCase) >= 0;

                    bool marcadoNota = (c.Tag as string)?.Equals("nota", StringComparison.OrdinalIgnoreCase) == true;

                    if (marcadoNota || c is Button || esGuna)
                    {
                        c.Click -= Nota_Click;
                        c.Click += Nota_Click;
                    }
                }

                if (c.HasChildren) WireNotaButtons(c);
            }
        }

        private void frmNHelados_Load(object sender, EventArgs e)
        {
            // “Q x PRODUCTO” arriba
            int q = Math.Max(1, Cantidad);
            string nombre = (Producto ?? "").ToUpperInvariant();
            ProductoSelectLine = $"{q} x {nombre}";
            txtProductoSelect.Text = ProductoSelectLine;

            // Cablea solo los botones de nota dentro del contenedor
            WireNotaButtons(flpNHelados);

            // Asegura multi-línea y Enter en Guna2TextBox
            txtNotasHelado.Multiline = true;
            txtNotasHelado.AcceptsReturn = true;
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmNHelados_ControlAdded(object sender, ControlEventArgs e)
        {
            WireNotaButtons(e.Control);
        }


        private void btnEliminar_Click(object sender, EventArgs e)
        {
            // elimina SOLO la última línea (compat. CR/LF)
            /*
            string s = (txtNotasHelado.Text ?? string.Empty).Replace("\r\n", "\n");
            s = s.TrimEnd('\n', '\r', ' ');
            int i = s.LastIndexOf('\n');
            s = (i >= 0) ? s.Substring(0, i) : string.Empty;
            txtNotasHelado.Text = s.Replace("\n", Environment.NewLine);
            txtNotasHelado.SelectionStart = txtNotasHelado.TextLength;
            txtNotasHelado.ScrollToCaret();*/

            if (_notas.Count == 0) return;
            _notas.RemoveAt(_notas.Count - 1);
            RedibujarNotas();
        }

        private void btnContinuar_Click(object sender, EventArgs e)
        {
            // por si el usuario editó el encabezado
            ProductoSelectLine = (txtProductoSelect.Text ?? "").Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
        /*
        private void MostrarDialogoNotas(ceProductos prod, int cantidad)
        {
            using (var dlg = new CapaPresentacion.Notas.frmNHelados())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;

                // Precarga datos para el diálogo
                dlg.Cantidad = cantidad;
                dlg.Producto = prod.Descripcion ?? prod.Codigo;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // En vez de escribir a txtProducto/txtNota -> agregamos una línea al panel
                    //AgregarLineaPedido(prod, cantidad, dlg.Notas);
                }
            }
        }*/

    }
}
