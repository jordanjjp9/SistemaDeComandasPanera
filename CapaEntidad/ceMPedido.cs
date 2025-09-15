using System;
using System.Collections.Generic;
using System.Linq;

namespace CapaEntidad
{
    public class ceMPedido
    {
        // === Parámetros comunes ===
        /// <summary>Tasa IGV en fracción (10% = 0.10).</summary>
        public const decimal IGV_TASA = 0.10m;

        // === M_PED (cabecera) ===
        public string NUM_PED { get; set; } = "";             // se asigna en DAOPedido
        public DateTime FEC_PED { get; set; } = DateTime.Now;
        public string CDG_VEND { get; set; } = "";             // mozo
        public string CDG_MON { get; set; } = "001";             // Soles
        public string CDG_LOC { get; set; } = "";              // no lo usamos al insertar (va '000'), pero lo dejamos
        public string CDG_AMB { get; set; } = "";              // 001/002/003 (salón/delivery/rappi)
        public string NUM_MESA { get; set; } = "";             // “004” (3 dígitos) lo pad-left hace el DAO
        public int? NUM_PERS { get; set; }                     // puede ser nulo
        public string CDG_USR { get; set; } = "";              // usuario app (del login)
        public string OBS_PED { get; set; } = "";              // comentario general (si lo hubiera)

        // Nuevos (los usa DAOPedido)
        public string RUC_CLI { get; set; } = "00000000";      // cliente por defecto
        public string CDG_CAJA { get; set; } = "";             // caja si aplica

        // Totales (se recalculan desde el detalle)
        /// <summary>Base imponible (sin IGV, suma de IMP_TPRD).</summary>
        public decimal IMP_BASE { get; set; }
        /// <summary>IGV total (suma de IMP_IGV).</summary>
        public decimal IMP_IGV { get; set; }
        /// <summary>Total con IGV (IMP_BASE + IMP_IGV).</summary>
        public decimal IMP_TOT { get; set; }

        /// <summary>Tasa IGV en fracción (ej. 0.10 para 10%).</summary>
        public decimal POR_IGV { get; set; } = IGV_TASA;

        // === D_PED (detalle) ===
        public List<ceDPedido> Detalles { get; set; } = new List<ceDPedido>();

        /// <summary>
        /// Recalcula Base/IGV/Total usando los importes ya calculados en cada detalle:
        ///   - IMP_BASE = Σ IMP_TPRD (sin IGV)
        ///   - IMP_IGV  = Σ IMP_IGV
        ///   - IMP_TOT  = IMP_BASE + IMP_IGV
        /// </summary>
        public void RecalcularTotales()
        {
            decimal baseSinIgv = Detalles.Sum(d => d.IMP_TPRD);
            decimal igvTotal = Detalles.Sum(d => d.IMP_IGV);

            // redondeo financiero a 2 decimales
            IMP_BASE = Math.Round(baseSinIgv, 2, MidpointRounding.AwayFromZero);
            IMP_IGV = Math.Round(igvTotal, 2, MidpointRounding.AwayFromZero);
            IMP_TOT = Math.Round(IMP_BASE + IMP_IGV, 2, MidpointRounding.AwayFromZero);
        }

      ///  public int NUM_PED { get; set; }
        public string CDG_MESA { get; set; }
        public string SWT_PED { get; set; } // "" o "T"
    }
}
