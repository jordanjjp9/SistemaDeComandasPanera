using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class cnProducto
    {

        private readonly DAOProductos _dao;

        public cnProducto()
        {
            _dao = new DAOProductos();
        }

        /// <summary>
        /// Lista básica para cachear en memoria (Código, Descripción, PrecioUnitario).
        /// Usada por frmMenuPrincipal para búsquedas instantáneas por CDG_PROD.
        /// </summary>
        public List<ceProductos> ListarBasico(string lprc = "001")
        {
            return _dao.ListarBasico(lprc);
        }

        /// <summary>
        /// Obtiene un producto por código dentro de una lista de precios.
        /// </summary>
        public ceProductos Obtener(string codigo, string lprc = "001")
        {
            return _dao.ObtenerPorCodigo(codigo, lprc);
        }

        /// <summary>
        /// Lista de productos por categoría/familia (trae unidad y precios).
        /// Útil para armar botoneras por familia.
        /// </summary>
        public List<ceProductos> ListarPorCategoria(string categoriaCod, string lprc = "001")
        {
            return _dao.ListarPorCategoria(categoriaCod, lprc);
        }

        /// <summary>
        /// DataTable preparado para DataGridView: CDG_PROD, PRODUCTO, UNM, PRECIO, CANTIDAD, TOTAL.
        /// Útil para formularios tipo “lista de productos” con edición de cantidad.
        /// </summary>
        public DataTable ListarParaVenta(string lprc = "001")
        {
            return _dao.ListarParaVenta(lprc);
        }

        private static readonly HashSet<string> _desayunoExcluidos =
                new HashSet<string>(StringComparer.Ordinal)
        {
            "0000000871","0000000875","0000001531","0000001248",
            "0000000832","0000000833","0000000818","0000000819"
        };

        public bool EsComboDesayuno(ceProductos p)
        {
            if (p == null) return false;

            string cod10 = (p.Codigo ?? "").Trim().PadLeft(10, '0');
            return string.Equals(p.TipoProductoCodigo, "039", StringComparison.Ordinal)
                   && !_desayunoExcluidos.Contains(cod10);
        }
    }
}
