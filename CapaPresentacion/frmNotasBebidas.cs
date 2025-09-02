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
    public partial class frmNotasBebidas : Form
    {
        
        // === Contexto recibido ===
        public string Producto { get; private set; } = "";
        public int CantidadProducto { get; private set; } = 1;

        // Notas que devolveremos al dueño
        public string NotaSeleccionada { get; private set; } = string.Empty;

        // Acumulador por opción (y orden de aparición)
        private readonly Dictionary<string, int> _cuentas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _orden = new List<string>();

        public frmNotasBebidas()
        {
            InitializeComponent();
            this.Load += frmNotasBebidas_Load;
        }

        public void SetContexto(string producto, int cantidad)
        {
            Producto = producto ?? "";
            CantidadProducto = Math.Max(1, cantidad);
            // Refresca encabezado
            if (txtProdSelect != null)
                txtProdSelect.Text = $"{CantidadProducto} x {Producto}".Trim();
        }

        // (opcional) para mostrar el nombre del producto en el título
        public string NombreProducto
        {
            get => this.Text;
            set => this.Text = string.IsNullOrWhiteSpace(value) ? "Notas de bebida" : $"Notas: {value}";
        }

        private void Opcion_Click(object sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                string opcion = (btn.Tag as string)?.Trim();
                if (string.IsNullOrWhiteSpace(opcion)) opcion = (btn.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(opcion)) return;

                // Suma 1 a la opción
                if (_cuentas.TryGetValue(opcion, out int count))
                {
                    _cuentas[opcion] = count + 1;
                }
                else
                {
                    _cuentas[opcion] = 1;
                    _orden.Add(opcion); // para mantener orden de primer click
                }

                RefrescarNotas();
            }
        }

        private void RefrescarNotas()
        {
            // Construye el texto: una línea por opción, en orden de aparición
            var lineas = new List<string>(_orden.Count);
            foreach (var op in _orden)
            {
                int c = _cuentas[op];
                lineas.Add($"{c} x {op}");
            }
            txtNotasBeb.Text = string.Join(Environment.NewLine, lineas);
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void frmNotasBebidas_Load(object sender, EventArgs e)
        {
            // Mapeamos cada botón de opción al mismo handler
            foreach (var btn in EnumerarBotonesOpciones(this))
            {
                // Guarda el texto original del botón en Tag para no perderlo
                if (btn.Tag == null) btn.Tag = btn.Text?.Trim();
                btn.Click -= Opcion_Click;
                btn.Click += Opcion_Click;
            }

            // Si no se llamó a SetContexto, al menos muestra el producto tal cual
            if (string.IsNullOrWhiteSpace(txtProdSelect.Text) && !string.IsNullOrWhiteSpace(Producto))
                txtProdSelect.Text = $"{CantidadProducto} x {Producto}".Trim();
        }

        private IEnumerable<Button> EnumerarBotonesOpciones(Control root)
        {
            // Si tienes un contenedor específico (por ejemplo pnlOpciones)
            // devuelve solo sus botones para evitar incluir “Cerrar” o “Continuar”.
            // Si tu contenedor se llama distinto, cámbialo aquí.
            Control cont = this.Controls["pnlOpciones"] ?? root;

            foreach (Control c in cont.Controls)
            {
                if (c is Button b &&
                    !string.Equals(b.Name, "btnCerrar", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(b.Name, "btnContinuar", StringComparison.OrdinalIgnoreCase))
                {
                    yield return b;
                }
                if (c.HasChildren)
                {
                    foreach (var b2 in EnumerarBotonesOpciones(c))
                        yield return b2;
                }
            }
        }

        private void btnContinuar_Click(object sender, EventArgs e)
        {
            // Envía lo que se ve en el TextBox (o lo reconstruimos si prefieres)
            NotaSeleccionada = txtNotasBeb.Text?.Trim() ?? "";
            this.DialogResult = DialogResult.OK;
        }
    }
}
