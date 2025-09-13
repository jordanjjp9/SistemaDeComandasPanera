using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaPresentacion.Impresion
{
    public class ComandaTicket
    {
        public string Ambiente { get; set; }
        public DateTime FechaHora { get; set; }
        public string NroPedido { get; set; }
        public int? NroPersonas { get; set; }
        public string Vendedor { get; set; }
        public string Mesa { get; set; }
        public List<Linea> Lineas { get; } = new List<Linea>();

        public class Linea
        {
            public decimal Cantidad { get; set; }
            public string NombreProducto { get; set; } = "";
            public string Notas { get; set; } = ""; // opcional
        }
    }
}
