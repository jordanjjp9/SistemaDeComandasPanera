using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class ceVendedor
    {
        public string Codigo { get; set; }   // CDG_VEND
        public string Nombre { get; set; }   // DES_VEND
        public bool Activo { get; set; }    // SWT_VEND == 1
    }
}
