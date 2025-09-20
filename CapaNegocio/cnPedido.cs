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
        private readonly DAOPedido _dao = new DAOPedido();

        // ===================== Guardar (insertar nuevo) =====================

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

            // Normalizaciones mínimas
            if (cabecera.FEC_PED == default(DateTime))
                cabecera.FEC_PED = DateTime.Now;

            if (string.IsNullOrWhiteSpace(cabecera.CDG_MON))
                cabecera.CDG_MON = "001"; // ajusta a tu esquema

            if (!string.IsNullOrEmpty(cabecera.CDG_AMB))
                cabecera.CDG_AMB = cabecera.CDG_AMB.Trim();

            if (!string.IsNullOrEmpty(cabecera.NUM_MESA))
                cabecera.NUM_MESA = cabecera.NUM_MESA.Trim().PadLeft(3, '0');

            // Asegurar totales consistentes antes de persistir (según reglas en ceMPedido)
            cabecera.RecalcularTotales();

            // Persistencia
            return _dao.InsertarPedido(cabecera, resolverImpresora, resolverTrib);
        }

        /// <summary>Conveniencia cuando no tienes resolutores.</summary>
        public string GuardarDb(ceMPedido cabecera)
        {
            return GuardarDb(cabecera, null, null);
        }

        // ===================== Anexar a pedido existente =====================

        /// <summary>
        /// Anexa SOLO los detalles enviados al pedido existente (NUM_PED),
        /// y recalcula totales en M_PEDIDO. No crea cabecera nueva.
        /// </summary>
        public void AnexarSoloDetalles(
            string numPed8,
            IList<ceDPedido> detallesNuevos,
            Func<string, string> resolverImpresora,
            Func<string, Tuple<decimal?, bool?>> resolverTrib
        )
        {
            if (string.IsNullOrWhiteSpace(numPed8))
                throw new ArgumentException("numPed vacío.", nameof(numPed8));

            if (detallesNuevos == null || detallesNuevos.Count == 0)
                return;

            // Normalizaciones mínimas por detalle
            foreach (var d in detallesNuevos)
            {
                if (!string.IsNullOrWhiteSpace(d.COD10))
                    d.COD10 = d.COD10.Trim().PadLeft(10, '0');

                if (d.CDG_PROD <= 0 && !string.IsNullOrWhiteSpace(d.COD10))
                    d.CDG_PROD = ceDPedido.CodigoToInt(d.COD10);

                if (d.CAN_PPRD <= 0)
                    d.CAN_PPRD = 1;

                if (d.OBS_PPRD == null)
                    d.OBS_PPRD = string.Empty;
            }

            _dao.AnexarSoloDetalles(numPed8.Trim(), detallesNuevos, resolverImpresora, resolverTrib);
        }

        /// <summary>Overload sin resolutores.</summary>
        public void AnexarSoloDetalles(string numPed8, IList<ceDPedido> detallesNuevos)
        {
            AnexarSoloDetalles(numPed8, detallesNuevos, null, null);
        }

        // ===================== Lecturas / Estado =====================

        /// <summary>Devuelve NUM_PED abierto por mesa (o null si no hay).</summary>
        public string ObtenerNumPedAbiertoPorMesa(string cdgMesa)
        {
            return _dao.ObtenerNumPedAbiertoPorMesa(cdgMesa);
        }

        /// <summary>¿El pedido sigue abierto? (SWT_PED '' o NULL).</summary>
        public bool PedidoSigueAbierto(string numPed8)
        {
            return _dao.PedidoSigueAbierto(numPed8);
        }

        /// <summary>Trae cabecera por NUM_PED.</summary>
        public ceMPedido ObtenerCabeceraPorNum(string numPed8)
        {
            return _dao.ObtenerCabeceraPorNum(numPed8);
        }

        /// <summary>Trae lista de detalles del pedido.</summary>
        public List<ceDPedido> ObtenerDetallePorPedido(string numPed8)
        {
            // DAOPedido ya propaga NUM_ITEM (formato 5 dígitos) y CDG_COMB al ceDPedido.
            return _dao.ObtenerDetallePorPedido(numPed8);
        }

        /// <summary>
        /// Helper: Dado un código de mesa, si tiene pedido abierto devuelve (numPed, cabecera, detalles).
        /// </summary>
        public Tuple<string, ceMPedido, List<ceDPedido>> CargarPedidoDeMesa(string cdgMesa)
        {
            string numPed = _dao.ObtenerNumPedAbiertoPorMesa(cdgMesa);
            if (string.IsNullOrEmpty(numPed))
                return Tuple.Create<string, ceMPedido, List<ceDPedido>>(null, null, new List<ceDPedido>());

            var cab = _dao.ObtenerCabeceraPorNum(numPed);
            var det = _dao.ObtenerDetallePorPedido(numPed);
            return Tuple.Create(numPed, cab, det);
        }

        // ===================== Eliminaciones =====================

        /// <summary>
        /// Elimina una línea por su CDG_FPRD (compatibilidad histórica; evita usarlo si puedes).
        /// </summary>
        public bool EliminarDetalle(string numPed, int cdgFprd)
        {
            return _dao.EliminarDetalle(numPed, cdgFprd) > 0;
        }

        /// <summary>
        /// Elimina por selección exacta: si llega CDG_COMB borra todo el combo/menú;
        /// si llega NUM_ITEM borra una fila puntual. Devuelve cantidad de filas afectadas.
        /// </summary>
        public int EliminarPorSeleccion(string numPed8, string cdgComb, string numItem)
        {
            if (string.IsNullOrWhiteSpace(numPed8)) return 0;

            // Normaliza: trims básicos (padding lo hace el DAO).
            cdgComb = string.IsNullOrWhiteSpace(cdgComb) ? null : cdgComb.Trim();
            numItem = string.IsNullOrWhiteSpace(numItem) ? null : numItem.Trim();

            if (cdgComb == null && numItem == null) return 0;

            return _dao.EliminarDetallesSeleccion(numPed8.Trim(), cdgComb, numItem);
        }

        /// <summary>
        /// Conveniencia: elimina todo un combo/menú por CDG_COMB.
        /// </summary>
        public int EliminarDetallePorCombo(string numPed8, string cdgComb)
        {
            if (string.IsNullOrWhiteSpace(numPed8) || string.IsNullOrWhiteSpace(cdgComb))
                return 0;

            return _dao.EliminarDetallePorCombo(numPed8.Trim(), cdgComb.Trim());
        }

        /// <summary>
        /// Conveniencia: elimina una única fila por NUM_ITEM (caso LineaPedidoItem).
        /// </summary>
        public int EliminarDetallePorNumItem(string numPed8, string numItem)
        {
            if (string.IsNullOrWhiteSpace(numPed8) || string.IsNullOrWhiteSpace(numItem))
                return 0;

            return _dao.EliminarDetallePorNumItem(numPed8.Trim(), numItem.Trim());
        }

        /// <summary>
        /// Alias semántico para UI: elimina una LineaPedidoItem dada por (NUM_PED, NUM_ITEM).
        /// </summary>
        public int EliminarLineaPedidoItem(string numPed8, string numItem)
        {
            return EliminarDetallePorNumItem(numPed8, numItem);
        }

        /// <summary>Recalcula base/igv/total en M_PEDIDO a partir de D_PEDIDO.</summary>
        public void RecalcularTotales(string numPed)
        {
            _dao.RecalcularTotales(numPed);
        }
    }
}
