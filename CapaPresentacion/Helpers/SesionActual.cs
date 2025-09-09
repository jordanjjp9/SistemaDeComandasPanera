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
        //// Vendedor validado
        //public static ceVendedor Vendedor { get; set; }

        //// Mesa seleccionada
        //public static ceMesa Mesa { get; set; }

        //// Ambiente (Salon, Delivery, Rappi)
        //public static string Ambiente { get; set; }

        //// Limpia los datos cuando sea necesario
        //public static void Limpiar()
        //{
        //    Vendedor = null;
        //    Mesa = null;
        //    Ambiente = null;
        //}
        // Vendedor validado (código digitado antes de entrar)
        public static ceVendedor Vendedor { get; set; }

        // Mesa seleccionada
        public static ceMesa Mesa { get; set; }

        // Ambiente (001 salón, 002 delivery, 003 rappi…)
        public static string Ambiente { get; set; }

        // NUEVO: usuario del login (CDG_USR)
        public static string Usuario { get; set; }

        // NUEVO: local/sucursal (CDG_LOC)
        public static string Local { get; set; }

        // NUEVO: caja (CDG_CAJA) si aplica
        public static string Caja { get; set; }

        // Limpia los datos cuando sea necesario
        public static void Limpiar()
        {
            Vendedor = null;
            Mesa = null;
            Ambiente = null;
            Usuario = null;
            Local = null;
            Caja = null;
        }
    }
}
