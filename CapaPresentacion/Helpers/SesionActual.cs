using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaEntidad;

namespace CapaPresentacion.Helpers
{
    public class SesionActual
    {
        // Vendedor validado
        public static ceVendedor Vendedor { get; set; }

        // Mesa seleccionada
        public static ceMesa Mesa { get; set; }

        // Ambiente (Salon, Delivery, Rappi)
        public static string Ambiente { get; set; }

        // Limpia los datos cuando sea necesario
        public static void Limpiar()
        {
            Vendedor = null;
            Mesa = null;
            Ambiente = null;
        }
    }
}
