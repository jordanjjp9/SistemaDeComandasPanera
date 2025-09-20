using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaEntidad
{
    public class DetalleRef
    {
        public string NumPed { get; set; }   // "00001378"
        public int    CdgFprd { get; set; }  // tu fallback histórico
        public string CdgComb { get; set; }  // "0000000101" si viene
        public string NumItem { get; set; }  // "00001"
    }
}
