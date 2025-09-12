using System;
using System.Collections.Generic;
using System.Data;
using CapaEntidad;
using CapaDatos;
using System.Linq;

namespace CapaNegocio
{
    /// <summary>
    /// Resultado de validación / login de vendedor-usuario.
    /// </summary>
    public class VendedorValidacionResult
    {
        public bool Ok { get; set; }
        public string Motivo { get; set; }        // null si Ok = true
        public ceVendedor Vendedor { get; set; }  // puede venir aunque esté inactivo para mostrar nombre
    }
    public class ActualizarUsrResult
    {
        public bool Ok { get; set; }
        public string Motivo { get; set; }        // por qué falló (si falla)
        public string CdgVend { get; set; }       // afectado
        public string NuevoUsr { get; set; }      // valor aplicado
    }
    public class cnVendedor
    {
        private readonly DAOVendedor _dao;

        public cnVendedor()
        {
            _dao = new DAOVendedor();
        }

        // ==================== EXISTENTES (POR CDG_VEND) ====================

        /// <summary>
        /// Valida un código de vendedor (CDG_VEND).
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
        /// Igual que Validar pero indicando el motivo cuando no es válido (por CDG_VEND).
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
        /// Obtiene un vendedor por CDG_VEND (sin validar activo).
        /// </summary>
        public ceVendedor Obtener(string codigo)
        {
            return _dao.ObtenerPorCodigo(codigo);
        }

        /// <summary>
        /// Devuelve el nombre si existe (y opcionalmente activo); null si no. (por CDG_VEND)
        /// </summary>
        public string ObtenerNombre(string codigo, bool soloActivos = true)
        {
            return _dao.ObtenerNombreSiExiste(codigo, soloActivos);
        }

        /// <summary>
        /// Indica si existe el vendedor (por CDG_VEND; opcionalmente solo activos).
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
        /// Cambia el estado activo/inactivo por CDG_VEND. Devuelve true si afectó filas.
        /// </summary>
        public bool CambiarEstado(string codigo, bool activo)
        {
            return _dao.ActualizarEstado(codigo, activo) > 0;
        }

        /// <summary>
        /// Tabla para formularios admin (CDG_VEND, DES_VEND, CDG_USR, SWT_VEND).
        /// </summary>
        public DataTable ListarTablaParaUsuarios(string filtro = null, bool? soloActivos = null)
            => _dao.ListarTablaParaUsuarios(filtro, soloActivos);

        // ==================== NUEVOS (POR CDG_USR) ====================

        /// <summary>
        /// Valida por CDG_USR. Devuelve el vendedor si existe (y opcionalmente activo), o null si no es válido.
        /// </summary>
        public ceVendedor ValidarPorUsr(string cdgUsr, bool soloActivos = true)
        {
            var v = _dao.ObtenerPorUsr(cdgUsr, soloActivos);
            if (v == null) return null;
            if (soloActivos && !v.Activo) return null;
            return v;
        }

        /// <summary>
        /// Igual que ValidarPorUsr pero indicando el motivo cuando no es válido.
        /// </summary>
        public VendedorValidacionResult ValidarConMotivoPorUsr(string cdgUsr, bool soloActivos = true)
        {
            // Intentamos obtener incluso si está inactivo para poder informar nombre/motivo.
            var v = _dao.ObtenerPorUsr(cdgUsr, soloActivos: false);

            if (v == null)
            {
                return new VendedorValidacionResult
                {
                    Ok = false,
                    Motivo = "Usuario inexistente.",
                    Vendedor = null
                };
            }

            if (soloActivos && !v.Activo)
            {
                return new VendedorValidacionResult
                {
                    Ok = false,
                    Motivo = "Usuario inactivo.",
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
        /// Obtiene un vendedor por CDG_USR (sin filtrar activo).
        /// </summary>
        public ceVendedor ObtenerPorUsr(string cdgUsr)
        {
            return _dao.ObtenerPorUsr(cdgUsr, soloActivos: false);
        }

        /// <summary>
        /// Indica si existe el CDG_USR (opcionalmente solo activos).
        /// </summary>
        public bool ExistePorUsr(string cdgUsr, bool soloActivos = true)
        {
            return _dao.ExistePorUsr(cdgUsr, soloActivos);
        }

        /// <summary>
        /// Login por CDG_USR (tu esquema no tiene PIN/CLAVE).
        /// Valida: existencia + (opcional) activo; devuelve motivo si falla.
        /// </summary>
        public VendedorValidacionResult LoginPorUsr(string cdgUsr, bool soloActivos = true)
        {
            // ¿Existe (sin filtrar activo para poder dar motivo)?
            var v = _dao.ObtenerPorUsr(cdgUsr, soloActivos: false);
            if (v == null)
            {
                return new VendedorValidacionResult
                {
                    Ok = false,
                    Motivo = "Usuario inexistente.",
                    Vendedor = null
                };
            }

            // ¿Activo si se requiere?
            if (soloActivos && !v.Activo)
            {
                return new VendedorValidacionResult
                {
                    Ok = false,
                    Motivo = "Usuario inactivo.",
                    Vendedor = v
                };
            }

            // Validación final por USR (sin PIN)
            if (!_dao.ValidarLoginPorUsr(cdgUsr))
            {
                return new VendedorValidacionResult
                {
                    Ok = false,
                    Motivo = "Usuario no autorizado.",
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

        public ActualizarUsrResult ActualizarUsrPorVend(string cdgVend, string nuevoUsr)
        {
            // Validaciones de entrada
            cdgVend = (cdgVend ?? "").Trim();
            nuevoUsr = (nuevoUsr ?? "").Trim();

            if (cdgVend.Length == 0)
                return new ActualizarUsrResult { Ok = false, Motivo = "CDG_VEND vacío.", CdgVend = cdgVend };

            // En tu formulario ya validas 4 dígitos, pero reforzamos aquí
            if (nuevoUsr.Length != 4 || !nuevoUsr.All(char.IsDigit))
                return new ActualizarUsrResult { Ok = false, Motivo = "CDG_USR debe tener exactamente 4 dígitos numéricos.", CdgVend = cdgVend };

            // 1) Verificar que el vendedor exista
            var vend = _dao.ObtenerPorCodigo(cdgVend);
            if (vend == null)
                return new ActualizarUsrResult { Ok = false, Motivo = "Vendedor inexistente.", CdgVend = cdgVend };

            // 2) Prevenir duplicados de CDG_USR: si ya existe y NO es el mismo vendedor → conflicto
            var otroConMismoUsr = _dao.ObtenerPorUsr(nuevoUsr, soloActivos: false);
            if (otroConMismoUsr != null && !string.Equals(otroConMismoUsr.Codigo, vend.Codigo, StringComparison.OrdinalIgnoreCase))
            {
                return new ActualizarUsrResult
                {
                    Ok = false,
                    Motivo = $"El CDG_USR {nuevoUsr} ya está asignado al vendedor {otroConMismoUsr.Codigo} - {otroConMismoUsr.Nombre}.",
                    CdgVend = cdgVend
                };
            }

            // 3) Actualizar en BD
            int filas = _dao.ActualizarUsrPorVend(cdgVend, nuevoUsr);
            if (filas == 0)
                return new ActualizarUsrResult { Ok = false, Motivo = "No se actualizó ninguna fila.", CdgVend = cdgVend };

            return new ActualizarUsrResult
            {
                Ok = true,
                Motivo = null,
                CdgVend = cdgVend,
                NuevoUsr = nuevoUsr
            };
        }
    }
}
