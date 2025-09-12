using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaEntidad;

namespace CapaPresentacion.Helpers
{
    // Enum simple para elegir el ambiente desde la UI
    public enum AmbienteTipo { Salon, Delivery, Rappi }

    public static class SesionActual
    {
        // --- Identidad de la sesión ---
        public static ceVendedor Vendedor { get; set; }   // mozo vigente
        public static ceMesa Mesa { get; set; }   // mesa seleccionada

        // Usuario / local / caja
        public static string Usuario { get; set; } = "VENTAS";
        public static string Local { get; set; } = "001";
        public static string Caja { get; set; } = "001";

        // Ambiente SIEMPRE como código de 3 dígitos (001/002/003)
        public static string Ambiente { get; private set; } = "001"; // default: SALÓN

        /// <summary>Fija el ambiente usando el enum (compatible C# 7.3).</summary>
        public static void SetAmbiente(AmbienteTipo tipo)
        {
            switch (tipo)
            {
                case AmbienteTipo.Salon:
                    Ambiente = "001";
                    break;
                case AmbienteTipo.Delivery:
                    Ambiente = "002";
                    break;
                case AmbienteTipo.Rappi:
                    Ambiente = "003";
                    break;
                default:
                    Ambiente = "001";
                    break;
            }
        }

        /// <summary>
        /// Fija el ambiente a partir del texto del botón/pestaña
        /// ("SALON", "DELIVERY", "RAPPI"). Ignora mayúsculas/minúsculas.
        /// </summary>
        public static void SetAmbiente(string nombre)
        {
            var t = (nombre ?? string.Empty).Trim().ToUpperInvariant();

            if (t.StartsWith("SAL"))
                Ambiente = "001";
            else if (t.StartsWith("DEL"))
                Ambiente = "002";
            else if (t.StartsWith("RAP"))
                Ambiente = "003";
            else
                Ambiente = "001";
        }

        // --- “Memoria” útil para precargar en el formulario ---
        public static string UltMesa { get; set; } // p.ej. "21", "807"
        public static int? UltPers { get; set; } // p.ej. 2, 3…
        public static string UltMozo
        {
            get { return Vendedor != null ? Vendedor.Codigo : null; }
        }

        // --- Reset de sesión ---
        public static void Limpiar()
        {
            Vendedor = null;
            Mesa = null;
            Usuario = null;
            Local = null;
            Caja = null;

            UltMesa = null;
            UltPers = null;

            // volvemos al default (SALÓN)
            Ambiente = "001";
        }
    }
}
