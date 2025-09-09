using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaEntidad;

namespace CapaDatos
{
    public class DAOPedidoTxt
    {
        /// <summary>
        /// Genera los TXT de M_PEDIDO y D_PEDIDO para el pedido dado.
        /// </summary>
        /// <param name="cab">Cabecera del pedido con su lista de detalles.</param>
        /// <param name="carpetaDestino">Carpeta donde se guardarán los archivos.</param>
        /// <param name="incluirEncabezados">Si true, escribe la primera línea con nombres de columnas.</param>
        public static (string mPath, string dPath) Exportar(ceMPedido cab, string carpetaDestino, bool incluirEncabezados = false)
        {
            if (cab == null) throw new ArgumentNullException(nameof(cab));
            if (cab.Detalles == null) cab.Detalles = new System.Collections.Generic.List<ceDPedido>();

            cab.RecalcularTotales();

            if (string.IsNullOrWhiteSpace(carpetaDestino))
                carpetaDestino = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            Directory.CreateDirectory(carpetaDestino);

            string suf = (cab.NUM_PED ?? "").PadLeft(8, '0');
            string mPath = Path.Combine(carpetaDestino, $"M_PEDIDO_{suf}.txt");
            string dPath = Path.Combine(carpetaDestino, $"D_PEDIDO_{suf}.txt");

            // ===== M_PEDIDO =====
            using (var wm = new StreamWriter(mPath, false, Encoding.UTF8))
            {
                if (incluirEncabezados)
                    wm.WriteLine("NUM_PED|FEC_PED|CDG_VEND|CDG_MON|CDG_LOC|CDG_AMB|NUM_MESA|NUM_PERS|POR_IGV|IMP_BASE|IMP_IGV|IMP_TOT|OBS_PED");

                string[] colsCab =
                {
                    cab.NUM_PED ?? "",
                    cab.FEC_PED.ToString("yyyy-MM-dd HH:mm:ss"),
                    cab.CDG_VEND ?? "",
                    cab.CDG_MON  ?? "S",
                    cab.CDG_LOC  ?? "",
                    cab.CDG_AMB  ?? "",
                    cab.NUM_MESA ?? "",
                    cab.NUM_PERS.HasValue ? cab.NUM_PERS.Value.ToString(CultureInfo.InvariantCulture) : "",
                    cab.POR_IGV.ToString("0.00", CultureInfo.InvariantCulture),
                    cab.IMP_BASE.ToString("0.00", CultureInfo.InvariantCulture),
                    cab.IMP_IGV .ToString("0.00", CultureInfo.InvariantCulture),
                    cab.IMP_TOT .ToString("0.00", CultureInfo.InvariantCulture),
                    string.IsNullOrWhiteSpace(cab.OBS_PED) ? "" : cab.OBS_PED
                };
                wm.WriteLine(string.Join("|", colsCab));
            }

            // ===== D_PEDIDO =====
            using (var wd = new StreamWriter(dPath, false, Encoding.UTF8))
            {
                if (incluirEncabezados)
                    wd.WriteLine("NUM_PED|CDG_PROD|CDG_FPRD|CAN_PPRD|PRE_PPRD|DCT_PPRD|DCT_FIC|IGV_PPRD|IMP_TPRD|CAN_DPRD|CAN_FPRD|OBS_PPRD|CDG_LPRC|PRE_IGV|IMP_IGV");

                foreach (var l in cab.Detalles)
                {
                    // CDG_PROD debe ir como 10 dígitos
                    string cdgProd10 = !string.IsNullOrWhiteSpace(l.COD10)
                        ? l.COD10.PadLeft(10, '0')
                        : l.CDG_PROD.ToString("0000000000", CultureInfo.InvariantCulture);

                    string obs = string.IsNullOrWhiteSpace(l.OBS_PPRD) ? "" : l.OBS_PPRD;

                    string[] colsDet =
                    {
                        cab.NUM_PED ?? "",                                              // NUM_PED
                        cdgProd10,                                                      // CDG_PROD (10 dígitos)
                        "",                                                             // CDG_FPRD (vacío)
                        l.CAN_PPRD.ToString("0.0000", CultureInfo.InvariantCulture),    // CAN_PPRD
                        l.PRE_PPRD.ToString("0.0000", CultureInfo.InvariantCulture),    // PRE_PPRD (SIN IGV)
                        "",                                                             // DCT_PPRD (vacío)
                        "",                                                             // DCT_FIC  (vacío)
                        "",                                                             // IGV_PPRD (vacío)
                        l.IMP_TPRD.ToString("0.00", CultureInfo.InvariantCulture),      // IMP_TPRD (SIN IGV)
                        "",                                                             // CAN_DPRD (vacío)
                        "",                                                             // CAN_FPRD (vacío)
                        obs,                                                            // OBS_PPRD
                        "001",                                                          // CDG_LPRC
                        l.PRE_IGV.ToString("0.0000", CultureInfo.InvariantCulture),     // PRE_IGV (CON IGV)
                        l.IMP_IGV.ToString("0.00", CultureInfo.InvariantCulture)        // IMP_IGV (CON IGV)
                    };

                    wd.WriteLine(string.Join("|", colsDet));
                }
            }

            return (mPath, dPath);
        }
    }
}
