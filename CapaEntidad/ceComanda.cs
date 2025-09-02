using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace CapaEntidad
{
    public class ceComanda
    {
        public DateTime Fecha { get; set; }
        public DateTime Hora { get; set; }
        public string Pedido { get; set; }
        public string Caja {  get; set; }
        public string Salon { get; set; }
        public string Mozo { get; set; }
        public string Mesa { get; set; }
        public string CantProducto { get; set; }
        public string Producto { get; set; }
        public string PrecioUnit { get; set; }
        public string Total { get; set; }
        public string CantTotalProd { get; set; }
    }
}
