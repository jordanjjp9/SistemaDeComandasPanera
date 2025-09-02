using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class ceMesa
    {
        public int Numero { get; set; }          // Ej. 1..21, 800..810
        public string Ambiente { get; set; }     // "SALON", "DELIVERY", "RAPPI"
        public bool Ocupada { get; set; }        // true si hay pedido abierto
        public string PedidoActual { get; set; } // NUM_PED si hay
    }
}
