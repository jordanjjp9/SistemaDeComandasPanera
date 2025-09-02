using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class ceProductos
    {
        public string Codigo { get; set; }          // CDG_PROD
        public string Descripcion { get; set; }     // DES_PROD
        public string UnidadCodigo { get; set; }    // CDG_UMED
        public string UnidadDescripcion { get; set; } // UMD
        public decimal ValorUnitario { get; set; }  // VAL_SOL
        public decimal PrecioUnitario { get; set; } // PRE_SOL
        public string ListaPrecioCodigo { get; set; } // LCDG_LPRC
        public bool Activo { get; set; }
        public string TipoProductoCodigo { get; set; }           // CDG_TPRD
    }
}
