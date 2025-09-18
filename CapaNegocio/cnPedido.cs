using System;
using System.Collections.Generic;
using System.Linq;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    /// <summary>
    /// Servicio de dominio para operaciones con pedidos.
    /// </summary>
    public class cnPedido
    {
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

            // ===== Normalizaciones mínimas =====
            if (cabecera.FEC_PED == default(DateTime))
                cabecera.FEC_PED = DateTime.Now;

            // En tu esquema la moneda operativa suele ser "S" o "001".
            if (string.IsNullOrWhiteSpace(cabecera.CDG_MON))
                cabecera.CDG_MON = "S";

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
        /// <param name="numPed8">Número de pedido (8 dígitos).</param>
        /// <param name="detallesNuevos">Renglones nuevos (tal como los arma TxtPedidoWriter).</param>
        /// <param name="resolverImpresora">Opcional: resolver IMP_PROD por COD10.</param>
        /// <param name="resolverTrib">Opcional: resolver POR_IGV/SWT_IGV por COD10.</param>
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

            // Normalizaciones mínimas sobre cada detalle (por si viniera algo flojo)
            foreach (var d in detallesNuevos)
            {
                // COD10 siempre con 10 posiciones si aplica
                if (!string.IsNullOrWhiteSpace(d.COD10))
                    d.COD10 = d.COD10.Trim().PadLeft(10, '0');

                // CDG_PROD desde COD10 si falta
                if (d.CDG_PROD <= 0 && !string.IsNullOrWhiteSpace(d.COD10))
                    d.CDG_PROD = ceDPedido.CodigoToInt(d.COD10);

                // Cantidad mínima = 1 (si viniera 0 o negativa)
                if (d.CAN_PPRD <= 0)
                    d.CAN_PPRD = 1;

                // Asegura OBS_PPRD no nula
                if (d.OBS_PPRD == null) d.OBS_PPRD = string.Empty;
            }

            // Delegamos TODO a DAO (transacción incluida):
            //  1) Verificar que existe M_PEDIDO (y que está abierto si tu regla lo exige).
            //  2) Insertar cada ceDPedido en D_PEDIDO con NUM_PED = numPed8
            //     - Completar PRE_IGV/IMP_IGV/PU si hace falta (aunque idealmente ya vienen listos).
            //     - Guardar OBS_PPRD tal como llega (con tags).
            //     - Asignar IMP_PROD con resolverImpresora si lo pasaste.
            //     - Aplicar resolverTrib (POR_IGV/SWT_IGV) si lo pasaste.
            //  3) Recalcular totales de M_PEDIDO sumando todo lo que hay en D_PEDIDO.
            //  4) (Opcional) Actualizar FEC_PED a GETDATE() si te interesa.
            var dao = new DAOPedido();
            dao.AnexarSoloDetalles(numPed8, detallesNuevos, resolverImpresora, resolverTrib);
        }

        /// <summary>
        /// Overload sin resolutores.
        /// </summary>
        public void AnexarSoloDetalles(string numPed8, IList<ceDPedido> detallesNuevos)
        {
            AnexarSoloDetalles(numPed8, detallesNuevos, null, null);
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
