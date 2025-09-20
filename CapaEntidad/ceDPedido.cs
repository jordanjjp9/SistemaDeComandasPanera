using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class ceDPedido
    {
        // === Claves de producto ===
        public string COD10 { get; set; } = "";   // "0000000462" (útil para la UI)
        public int CDG_PROD { get; set; }         // 462 (para D_PED/BD)
        public int CDG_FPRD { get; set; } = 0;
        public string NUM_ITEM { get; set; }   // correlativo/fila textual

        // === Cantidad y precios ===
        public decimal CAN_PPRD { get; set; } = 1;
        public decimal PRE_PPRD { get; set; }        // SIN IGV (4 dec)
        public decimal IMP_TPRD { get; set; }        // importe SIN IGV (2 dec)
        public decimal PRE_IGV { get; set; }        // CON IGV (2 dec)
        public decimal IMP_IGV { get; set; }        // importe CON IGV (2 dec)

        // === Otros D_PED ===
        public string CDG_COMB { get; set; }  // id de grupo (p.ej. "100","101"...). El DAO lo acolcha a 10 dígitos.

        public string OBS_PPRD { get; set; } = "";  // dejar BLANCO si no hay (no "0")
        public int CDG_LPRC { get; set; } = 1;   // Lista 001
        public string IMP_PROD { get; set; } = "";  // impresora (si aplica)
        public bool? SWT_IMPR { get; set; }        // impresora activa (nullable)

        // === Meta-UI (no BD) ===
        public string DESCRIPCION { get; set; } = "";

        // === Helpers ===

        /// <summary>
        /// Completa PRE_PPRD/PRE_IGV/IMP_TPRD/IMP_IGV partiendo de un PU CON IGV (el de la UI).
        /// IGV: igvTasa (ej. 0.10m). Respeta redondeos como tu BD y costo 0.
        /// </summary>
        public void SetPreciosDesdePuConIgv(decimal puConIgv, decimal igvTasa)
        {
            decimal preConIgv2 = Math.Round(puConIgv, 2, MidpointRounding.AwayFromZero);
            decimal preSinIgv4 = Math.Round(preConIgv2 / (1m + igvTasa), 4, MidpointRounding.AwayFromZero);

            PRE_PPRD = preSinIgv4;
            PRE_IGV = preConIgv2;

            IMP_TPRD = Math.Round(Math.Round(PRE_PPRD, 2, MidpointRounding.AwayFromZero) * CAN_PPRD, 2, MidpointRounding.AwayFromZero);
            IMP_IGV = Math.Round(PRE_IGV * CAN_PPRD, 2, MidpointRounding.AwayFromZero);

            if (PRE_IGV == 0m) { PRE_PPRD = 0m; IMP_TPRD = 0m; IMP_IGV = 0m; } // costo 0
        }

        /// <summary>Convierte "0000000462" → 462 (cómodo al poblar CDG_PROD).</summary>
        public static int CodigoToInt(string cod10)
        {
            if (string.IsNullOrWhiteSpace(cod10)) return 0;
            int n;
            return int.TryParse(cod10.TrimStart('0'), out n) ? n : 0;
        }
    }
}
