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

namespace CapaPresentacion
{
    public partial class ucLineaPedido : UserControl
    {
        public string Codigo { get; private set; }
        public string Descripcion { get; private set; }
        public decimal PrecioUnitario { get; private set; }
        public int Cantidad { get; private set; } = 1;

        // Expones la Nota directamente desde txtNot

        public string Nota
        {
            get => txtNot.Text;
            set => txtNot.Text = value ?? string.Empty;
        }

        public string ProductoText
        {
            get => txtProd.Text;
            set => txtProd.Text = value ?? string.Empty;
        }

        /// <summary>
        /// Acceso directo a la nota (alias de Nota). Lo dejo por comodidad/simetría.
        /// </summary>
        public string NotaText
        {
            get => Nota;
            set => Nota = value;
        }

        public ucLineaPedido()
        {
            InitializeComponent();
        }
        public void Bind(ceProductos prod)
        {
            if (prod == null) return;

            Codigo = prod.Codigo?.Trim();
            Descripcion = prod.Descripcion?.Trim();
            PrecioUnitario = prod.PrecioUnitario;
            Cantidad = 1;

            RefrescarUI();
        }

        /// <summary>
        /// Ajusta la cantidad (mínimo 1) y refresca la UI.
        /// </summary>
        public void SetCantidad(int cantidad)
        {
            Cantidad = Math.Max(1, cantidad);
            RefrescarUI();
        }

        /// <summary>
        /// Incrementa/disminuye cantidad (delta puede ser negativo) y refresca.
        /// </summary>
        public void Incrementar(int delta = 1)
        {
            SetCantidad(Cantidad + delta);
        }

        /// <summary>
        /// Redibuja el texto del producto con el formato pedido:
        /// "Cantidad x Descripción - S/ PrecioUnitario"
        /// </summary>
        private void RefrescarUI()
        {
            // Si no estás usando Bind y sólo quieres poner texto a mano,
            // puedes ignorar este formato y usar ProductoText directamente.
            if (!string.IsNullOrWhiteSpace(Descripcion))
            {
                txtProd.Text = $"{Cantidad} x {Descripcion} - S/ {PrecioUnitario:N2}";
            }
        }
    }
}
