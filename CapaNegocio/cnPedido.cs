using System;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Servicio de dominio para operaciones con pedidos.
    /// </summary>
    public class cnPedido
    {
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
                throw new ArgumentNullException("cabecera");

            if (cabecera.Detalles == null || cabecera.Detalles.Count == 0)
                throw new InvalidOperationException("El pedido no contiene detalles.");

            // ===== Normalizaciones mínimas =====
            if (cabecera.FEC_PED == default(DateTime))
                cabecera.FEC_PED = DateTime.Now;

            if (string.IsNullOrWhiteSpace(cabecera.CDG_MON))
                cabecera.CDG_MON = "S"; // Soles

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
    }
}
