using System;
using System.Collections.Generic;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Servicio de dominio para operaciones con pedidos.
    /// </summary>
    public class cnPedido
    {
        // ===================== Guardar =====================

        /// <summary>
        /// Guarda el pedido en BD (M_PEDIDO + D_PEDIDO) usando DAOPedido dentro
        /// de una transacción. Devuelve el NUM_PED (8 dígitos) generado.
        /// </summary>
        /// <param name="cabecera">Pedido armado con sus detalles.</param>
        /// <param name="resolverImpresora">
        /// Func que recibe COD10 de producto y devuelve el código de impresora (p.ej. "003") o "".
        /// Puede ser null si no aplica.
        /// </param>
        /// <param name="resolverTrib">
        /// Func opcional que recibe COD10 y devuelve Tuple(porIgvDecimal, swtIgvBool) o null.
        /// </param>
        public string GuardarDb(
            ceMPedido cabecera,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib
        )
        {
            if (cabecera == null)
                throw new ArgumentNullException(nameof(cabecera));

            if (cabecera.Detalles == null || cabecera.Detalles.Count == 0)
                throw new InvalidOperationException("El pedido no contiene detalles.");

            // ===== Normalizaciones mínimas =====
            if (cabecera.FEC_PED == default(DateTime))
                cabecera.FEC_PED = DateTime.Now;

            if (string.IsNullOrWhiteSpace(cabecera.CDG_MON))
                cabecera.CDG_MON = "001"; // Moneda por defecto que usa el DAO (Soles)

            if (!string.IsNullOrEmpty(cabecera.CDG_AMB))
                cabecera.CDG_AMB = cabecera.CDG_AMB.Trim();

            if (!string.IsNullOrEmpty(cabecera.NUM_MESA))
                cabecera.NUM_MESA = cabecera.NUM_MESA.Trim().PadLeft(3, '0');

            // Asegurar totales consistentes (base/igv/total) antes de persistir
            cabecera.RecalcularTotales();

            // ===== Persistencia =====
            var dao = new DAOPedido();
            string numPed = dao.InsertarPedido(cabecera, resolverImpresora, resolverTrib);

            return numPed;
        }

        /// <summary>
        /// Sobrecarga cómoda cuando no tienes resolutores.
        /// </summary>
        public string GuardarDb(ceMPedido cabecera)
        {
            return GuardarDb(cabecera, null, null);
        }

        // ===================== Lecturas / Estado =====================

        /// <summary>
        /// Devuelve el NUM_PED (8 dígitos) del pedido ABIERTO de una mesa (SWT_PED '' o NULL).
        /// Si no hay, devuelve null.
        /// </summary>
        public string ObtenerNumPedAbiertoPorMesa(string cdgMesa)
        {
            var dao = new DAOPedido();
            return dao.ObtenerNumPedAbiertoPorMesa(cdgMesa);
        }

        /// <summary>
        /// ¿El pedido sigue abierto? (SWT_PED '' o NULL).
        /// </summary>
        public bool PedidoSigueAbierto(string numPed8)
        {
            var dao = new DAOPedido();
            return dao.PedidoSigueAbierto(numPed8);
        }

        /// <summary>
        /// Trae cabecera básica por NUM_PED (para mostrar info en pantalla).
        /// </summary>
        public ceMPedido ObtenerCabeceraPorNum(string numPed8)
        {
            var dao = new DAOPedido();
            return dao.ObtenerCabeceraPorNum(numPed8);
        }

        /// <summary>
        /// Trae lista de detalles (D_PEDIDO) de un NUM_PED (para reingresar a la mesa).
        /// </summary>
        public List<ceDPedido> ObtenerDetallePorPedido(string numPed8)
        {
            var dao = new DAOPedido();
            return dao.ObtenerDetallePorPedido(numPed8);
        }

        /// <summary>
        /// Helper: Dado un código de mesa, si tiene pedido abierto devuelve
        /// (numPed, cabecera, detalles). Si no, numPed = null y listas vacías.
        /// Útil para el click del botón de la mesa.
        /// </summary>
        public Tuple<string, ceMPedido, List<ceDPedido>> CargarPedidoDeMesa(string cdgMesa)
        {
            var dao = new DAOPedido();

            string numPed = dao.ObtenerNumPedAbiertoPorMesa(cdgMesa);
            if (string.IsNullOrEmpty(numPed))
                return Tuple.Create<string, ceMPedido, List<ceDPedido>>(null, null, new List<ceDPedido>());

            var cab = dao.ObtenerCabeceraPorNum(numPed);
            var det = dao.ObtenerDetallePorPedido(numPed);

            return Tuple.Create(numPed, cab, det);
        }
    }
}
