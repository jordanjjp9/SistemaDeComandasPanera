using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using CapaDatos;
using CapaEntidad;

namespace CapaNegocio
{
    public class ActualizarImpresoraResult
    {
        public bool Ok { get; set; }
        public string Motivo { get; set; }
        public string CdgProd { get; set; }
        public string CdgImp { get; set; } // "001"
    }

    /// <summary>
    /// Lógica de negocio para mapeo y ruteo de impresoras.
    /// </summary>
    public class cnImpresora
    {
        private readonly DAOImpresora _dao = new DAOImpresora();

        // ===== Listados para UI admin =====
        public DataTable ListarProductosGrid4(string cdgLprc = "001")
            => _dao.ListarProductosGrid4(cdgLprc);

        //public DataTable ListarProductosConFormato(string cdgLprc = "001")
        //    => _dao.ListarProductosConFormato(cdgLprc);
        public DataTable ListarProductosConFormato() => _dao.ListarProductosConFormato();              // ← SIN filtro (tu SELECT exacto)

        public DataTable ListarFormasImpresora()
            => _dao.ListarFormasImpresora();

        // ===== Actualización de impresora secundaria =====
        public bool GuardarImpresoraSecundaria(string cdgProd, string codFormato)
            => _dao.ActualizarImpresoraSec(cdgProd, codFormato) > 0;

        public bool QuitarImpresoraSecundaria(string cdgProd)
            => _dao.ActualizarImpresoraSec(cdgProd, null) > 0;

        public ActualizarImpresoraResult ActualizarImpresoraSecundaria(string cdgProd, string codFormato)
        {
            cdgProd = (cdgProd ?? string.Empty).Trim();
            codFormato = (codFormato ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(cdgProd))
                return new ActualizarImpresoraResult { Ok = false, Motivo = "CDG_PROD vacío.", CdgProd = cdgProd };

            if (string.IsNullOrEmpty(codFormato))
            {
                var n = _dao.ActualizarImpresoraSec(cdgProd, null);
                return new ActualizarImpresoraResult { Ok = n > 0, Motivo = n > 0 ? null : "No se actualizó ninguna fila.", CdgProd = cdgProd, CdgImp = null };
            }

            if (!codFormato.All(char.IsDigit) || codFormato.Length > 3)
                return new ActualizarImpresoraResult { Ok = false, Motivo = "Código inválido: use hasta 3 dígitos numéricos.", CdgProd = cdgProd, CdgImp = codFormato };

            codFormato = codFormato.PadLeft(3, '0');

            var filas = _dao.ActualizarImpresoraSec(cdgProd, codFormato);
            return new ActualizarImpresoraResult { Ok = filas > 0, Motivo = filas > 0 ? null : "No se actualizó ninguna fila.", CdgProd = cdgProd, CdgImp = codFormato };
        }

        // ===== Ruteo e impresión (POS) =====

        /// <summary>
        /// Devuelve el nombre de impresora Windows para un CDG_FORM (p.ej. "003" = COCINA).
        /// Solo lee App.config del equipo: AppSettings["Impresora:003"].
        /// </summary>
        public string ResolverNombreImpresora(string cdgForm)
        {
            cdgForm = (cdgForm ?? string.Empty).Trim().PadLeft(3, '0');
            try
            {
                var key = "Impresora:" + cdgForm;
                var nombre = ConfigurationManager.AppSettings[key];
                if (!string.IsNullOrWhiteSpace(nombre)) return nombre;
            }
            catch { }
            return null; // que la UI avise y no intente imprimir
        }

        /// <summary>
        /// Agrupa las líneas por destino de impresión (principal y secundaria).
        /// Clave = CDG_FORM ("001", "003", etc.).
        /// </summary>
        public Dictionary<string, List<ceDPedido>> AgruparPorDestino(IEnumerable<ceDPedido> dets)
        {
            var porDestino = new Dictionary<string, List<ceDPedido>>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var d in dets)
            {
                var ruteo = DAOProductos.ObtenerRuteoPorProducto(d.CDG_PROD); // (impPrin, impSec)

                if (!string.IsNullOrWhiteSpace(ruteo.ImpPrin))
                {
                    List<ceDPedido> l1;
                    if (!porDestino.TryGetValue(ruteo.ImpPrin, out l1)) { l1 = new List<ceDPedido>(); porDestino[ruteo.ImpPrin] = l1; }
                    l1.Add(d);
                }
                if (!string.IsNullOrWhiteSpace(ruteo.ImpSec))
                {
                    List<ceDPedido> l2;
                    if (!porDestino.TryGetValue(ruteo.ImpSec, out l2)) { l2 = new List<ceDPedido>(); porDestino[ruteo.ImpSec] = l2; }
                    l2.Add(d);
                }
            }
            return porDestino;
        }
    }
}
