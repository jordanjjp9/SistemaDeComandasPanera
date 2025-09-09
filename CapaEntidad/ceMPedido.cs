using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class ceMPedido
    {
        // === Parámetros comunes ===
        public const decimal IGV_TASA = 0.10m;  // Lista 001 (10%)

        // === M_PED (cabecera) ===
        public string NUM_PED { get; set; } = "";         // con ceros a la izquierda si aplica
        public DateTime FEC_PED { get; set; } = DateTime.Now;
        public string CDG_VEND { get; set; } = "";
        public string CDG_MON { get; set; } = "S";        // Soles
        public string CDG_LOC { get; set; } = "";
        public string CDG_AMB { get; set; } = "";
        public string NUM_MESA { get; set; } = "";
        public int? NUM_PERS { get; set; }               // puede ser nulo
        public string CDG_USR { get; set; } = "";         // opcional (usuario app)
        public string OBS_PED { get; set; } = "";         // comentario general (si lo hubiera)

        // Totales (se recalculan desde el detalle)
        public decimal IMP_BASE { get; set; }               // base imponible (sin IGV)
        public decimal IMP_IGV { get; set; }               // IGV total
        public decimal IMP_TOT { get; set; }               // total con IGV
        public decimal POR_IGV { get; set; } = IGV_TASA;

        // === D_PED (detalle) ===
        public List<ceDPedido> Detalles { get; set; } = new List<ceDPedido>();

        /// <summary>Recalcula Base/IGV/Total usando la tasa POR_IGV y los importes del detalle.</summary>
        public void RecalcularTotales()
        {
            decimal totalConIgv = Detalles.Sum(d => d.IMP_IGV);
            IMP_TOT = Math.Round(totalConIgv, 2, MidpointRounding.AwayFromZero);
            IMP_IGV = Math.Round(IMP_TOT * POR_IGV / (1m + POR_IGV), 2, MidpointRounding.AwayFromZero);
            IMP_BASE = IMP_TOT - IMP_IGV;
        }
    }
}
