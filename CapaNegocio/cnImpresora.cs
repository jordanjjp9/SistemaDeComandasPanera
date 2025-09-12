using System.Data;
using System.Linq;
using CapaDatos;

namespace CapaNegocio
{
    public class ActualizarImpresoraResult
    {
        public bool Ok { get; set; }
        public string Motivo { get; set; }
        public string CdgProd { get; set; }
        public string CdgImp { get; set; } // "001"
    }

    public class cnImpresora
    {
        private readonly DAOImpresora _dao = new DAOImpresora();

        /// <summary>
        /// Para el grid de 4 columnas (nombres): CDG_PROD | Producto | ImprePrin | ImpreSec.
        /// </summary>
        public DataTable ListarProductosGrid4(string cdgLprc = "001")
            => _dao.ListarProductosGrid4(cdgLprc);

        /// <summary>
        /// Compatibilidad (6 columnas): CDG_PROD, DES_PROD, IMP_PROD, DES_FORM_PRN, CDG_IMP, DES_FORM_SEC.
        /// </summary>
        public DataTable ListarProductosConFormato(string cdgLprc = "001")
            => _dao.ListarProductosConFormato(cdgLprc);

        /// <summary>
        /// Catálogo de formatos/impresoras para combos (Value=CDG_FORM, Display=DES_FORM).
        /// </summary>
        public DataTable ListarFormasImpresora()
            => _dao.ListarFormasImpresora();

        /// <summary>
        /// Guarda el código de impresora secundaria (CDG_IMP).
        /// </summary>
        public bool GuardarImpresoraSecundaria(string cdgProd, string codFormato)
            => _dao.ActualizarImpresoraSec(cdgProd, codFormato) > 0;

        /// <summary>
        /// Limpia la impresora secundaria (NULL en CDG_IMP).
        /// </summary>
        public bool QuitarImpresoraSecundaria(string cdgProd)
            => _dao.ActualizarImpresoraSec(cdgProd, null) > 0;

        /// <summary>
        /// Versión con validaciones y motivo.
        /// </summary>
        public ActualizarImpresoraResult ActualizarImpresoraSecundaria(string cdgProd, string codFormato)
        {
            cdgProd = (cdgProd ?? string.Empty).Trim();
            codFormato = (codFormato ?? string.Empty).Trim();

            if (string.IsNullOrEmpty(cdgProd))
                return new ActualizarImpresoraResult
                {
                    Ok = false,
                    Motivo = "CDG_PROD vacío.",
                    CdgProd = cdgProd
                };

            if (string.IsNullOrEmpty(codFormato))
            {
                var n = _dao.ActualizarImpresoraSec(cdgProd, null);
                return new ActualizarImpresoraResult
                {
                    Ok = n > 0,
                    Motivo = n > 0 ? null : "No se actualizó ninguna fila.",
                    CdgProd = cdgProd,
                    CdgImp = null
                };
            }

            // Validación + normalización a 3 dígitos
            if (!codFormato.All(char.IsDigit) || codFormato.Length > 3)
            {
                return new ActualizarImpresoraResult
                {
                    Ok = false,
                    Motivo = "Código inválido: use hasta 3 dígitos numéricos.",
                    CdgProd = cdgProd,
                    CdgImp = codFormato
                };
            }
            codFormato = codFormato.PadLeft(3, '0');

            var filas = _dao.ActualizarImpresoraSec(cdgProd, codFormato);
            return new ActualizarImpresoraResult
            {
                Ok = filas > 0,
                Motivo = filas > 0 ? null : "No se actualizó ninguna fila.",
                CdgProd = cdgProd,
                CdgImp = codFormato
            };
        }
    }
}
