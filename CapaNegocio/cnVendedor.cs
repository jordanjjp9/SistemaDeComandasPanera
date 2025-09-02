using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaEntidad;
using CapaDatos;

namespace CapaNegocio
{
    /// <summary>
    /// Resultado de validación de vendedor.
    /// </summary>
    public class VendedorValidacionResult
    {
        public bool Ok { get; set; }
        public string Motivo { get; set; }      // null si Ok = true
        public ceVendedor Vendedor { get; set; } // puede venir aunque esté inactivo para mostrar nombre
    }

    public class cnVendedor
    {
        private readonly DAOVendedor _dao;

        public cnVendedor()
        {
            _dao = new DAOVendedor();
        }

        /// <summary>
        /// Valida un código de vendedor.
        /// Devuelve el vendedor si existe (y opcionalmente está activo), o null si no es válido.
        /// </summary>
        public ceVendedor Validar(string codigo, bool soloActivos = true)
        {
            var v = _dao.ObtenerPorCodigo(codigo);
            if (v == null) return null;
            if (soloActivos && !v.Activo) return null;
            return v;
        }

        /// <summary>
        /// Igual que Validar pero indicando el motivo cuando no es válido.
        /// </summary>
        public VendedorValidacionResult ValidarConMotivo(string codigo, bool soloActivos = true)
        {
            var v = _dao.ObtenerPorCodigo(codigo);

            if (v == null)
            {
                return new VendedorValidacionResult
                {
                    Ok = false,
                    Motivo = "Código inexistente.",
                    Vendedor = null
                };
            }

            if (soloActivos && !v.Activo)
            {
                return new VendedorValidacionResult
                {
                    Ok = false,
                    Motivo = "Vendedor inactivo.",
                    Vendedor = v
                };
            }

            return new VendedorValidacionResult
            {
                Ok = true,
                Motivo = null,
                Vendedor = v
            };
        }

        /// <summary>
        /// Obtiene un vendedor por código (sin validar activo).
        /// </summary>
        public ceVendedor Obtener(string codigo)
        {
            return _dao.ObtenerPorCodigo(codigo);
        }

        /// <summary>
        /// Devuelve el nombre si existe (y opcionalmente activo); null si no.
        /// </summary>
        public string ObtenerNombre(string codigo, bool soloActivos = true)
        {
            return _dao.ObtenerNombreSiExiste(codigo, soloActivos);
        }

        /// <summary>
        /// Indica si existe el vendedor (opcionalmente solo activos).
        /// </summary>
        public bool Existe(string codigo, bool soloActivos = true)
        {
            return _dao.Existe(codigo, soloActivos);
        }

        /// <summary>
        /// Lista vendedores con filtro y estado.
        /// </summary>
        public List<ceVendedor> Listar(string filtro = null, bool? soloActivos = null)
        {
            return _dao.Listar(filtro, soloActivos);
        }

        /// <summary>
        /// Cambia el estado activo/inactivo. Devuelve true si afectó filas.
        /// </summary>
        public bool CambiarEstado(string codigo, bool activo)
        {
            return _dao.ActualizarEstado(codigo, activo) > 0;
        }
    }
}
