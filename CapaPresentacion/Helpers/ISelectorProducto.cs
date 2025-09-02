using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaPresentacion.Helpers
{
    public interface ISelectorProducto
    {
        /// <summary>Se dispara cuando el usuario hace click en un botón de producto</summary>
        event Action<string> ProductoSeleccionado; // parámetro = CDG_PROD
    }
}
