using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class cnPedido
    {
        /// <summary>
        /// Exporta M_PEDIDO y D_PEDIDO a TXT usando la estructura ceMPedido/ceDPedido.
        /// </summary>
        public (string mPath, string dPath) ExportarTxt(ceMPedido cabecera,string carpetaDestino,bool incluirEncabezados = false)   // <— añade este parámetro (default false)
        {
            if (cabecera == null) throw new ArgumentNullException(nameof(cabecera));
            cabecera.RecalcularTotales();

            // <— pasa el flag al DAO
            return DAOPedidoTxt.Exportar(cabecera, carpetaDestino, incluirEncabezados);
        }
    }
}
