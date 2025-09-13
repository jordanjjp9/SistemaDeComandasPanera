using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace CapaDatos
{
    /// <summary>
    /// Acceso a datos para mapeo de productos ↔ impresoras (principal/secundaria)
    /// y catálogo de formatos/impresoras.
    /// </summary>
    public class DAOImpresora
    {
        private readonly string _cs;

        public DAOImpresora()
        {
            _cs = ConfigurationManager.ConnectionStrings["conexCholo"].ConnectionString;
        }

        private static string Norm3(string s) => (s ?? string.Empty).Trim().PadLeft(3, '0');

        /// <summary>
        /// PARA EL GRID (4 columnas): CDG_PROD | Producto | ImprePrin | ImpreSec (nombres).
        /// </summary>
        public DataTable ListarProductosGrid4(string cdgLprc)
        {
            var dt = new DataTable();

            const string sql = @"
            SELECT
                LTRIM(RTRIM(mprec.CDG_PROD)) AS CDG_PROD,
                LTRIM(RTRIM(mprod.DES_PROD)) AS Producto,
                LTRIM(RTRIM(f1.DES_FORM))    AS ImprePrin,  -- nombre impresora principal (lógico)
                LTRIM(RTRIM(f2.DES_FORM))    AS ImpreSec    -- nombre impresora secundaria (lógico)
            FROM dbo.M_PRECIO  AS mprec
            JOIN dbo.M_PRODUC  AS mprod ON mprod.CDG_PROD = mprec.CDG_PROD
            LEFT JOIN dbo.M_FRMIMP AS f1 ON f1.CDG_FORM = mprod.IMP_PROD
            LEFT JOIN dbo.M_FRMIMP AS f2 ON f2.CDG_FORM = mprod.CDG_IMP
            WHERE mprec.CDG_LPRC = @lprc
            ORDER BY mprod.DES_PROD;";

            using (var cn = new SqlConnection(_cs))
            using (var da = new SqlDataAdapter(sql, cn))
            {
                da.SelectCommand.Parameters.Add("@lprc", SqlDbType.VarChar, 3).Value = Norm3(cdgLprc);
                da.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// COMPATIBILIDAD (6 columnas): CDG_PROD, DES_PROD, IMP_PROD, DES_FORM_PRN, CDG_IMP, DES_FORM_SEC.
        /// </summary>
        public DataTable ListarProductosConFormato(string cdgLprc)
        {
            var dt = new DataTable();

            const string sql = @"
            SELECT 
                LTRIM(RTRIM(mprec.CDG_PROD)) AS CDG_PROD,
                LTRIM(RTRIM(mprod.DES_PROD)) AS DES_PROD,
                LTRIM(RTRIM(mprod.IMP_PROD)) AS IMP_PROD,
                LTRIM(RTRIM(f1.DES_FORM))    AS DES_FORM_PRN,
                LTRIM(RTRIM(mprod.CDG_IMP))  AS CDG_IMP,
                LTRIM(RTRIM(f2.DES_FORM))    AS DES_FORM_SEC
            FROM dbo.M_PRECIO  AS mprec
            JOIN dbo.M_PRODUC  AS mprod ON mprod.CDG_PROD = mprec.CDG_PROD
            LEFT JOIN dbo.M_FRMIMP AS f1 ON f1.CDG_FORM = mprod.IMP_PROD
            LEFT JOIN dbo.M_FRMIMP AS f2 ON f2.CDG_FORM = mprod.CDG_IMP
            WHERE mprec.CDG_LPRC = @lprc
            ORDER BY mprod.DES_PROD;";

            using (var cn = new SqlConnection(_cs))
            using (var da = new SqlDataAdapter(sql, cn))
            {
                da.SelectCommand.Parameters.Add("@lprc", SqlDbType.VarChar, 3).Value = Norm3(cdgLprc);
                da.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// Catálogo de formatos/impresoras (Value=CDG_FORM, Display=DES_FORM).
        /// </summary>
        public DataTable ListarFormasImpresora()
        {
            var dt = new DataTable();

            const string sql = @"
            SELECT 
                LTRIM(RTRIM(CDG_FORM)) AS CDG_FORM,
                LTRIM(RTRIM(DES_FORM)) AS DES_FORM
            FROM dbo.M_FRMIMP
            ORDER BY DES_FORM;";

            using (var cn = new SqlConnection(_cs))
            using (var da = new SqlDataAdapter(sql, cn))
            {
                da.Fill(dt);
            }

            return dt;
        }

        /// <summary>
        /// Actualiza M_PRODUC.CDG_IMP (impresora secundaria). Si viene vacío, guarda NULL.
        /// </summary>
        public int ActualizarImpresoraSec(string cdgProd, string cdgImp)
        {
            const string sql = @"
            UPDATE dbo.M_PRODUC
               SET CDG_IMP = @cdgImp
             WHERE CDG_PROD = @cdgProd;";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@cdgProd", SqlDbType.VarChar, 10)
                               .Value = (cdgProd ?? string.Empty).Trim();

                object valImp = string.IsNullOrWhiteSpace(cdgImp)
                    ? (object)DBNull.Value
                    : Norm3(cdgImp);

                cmd.Parameters.Add("@cdgImp", SqlDbType.VarChar, 3).Value = valImp;

                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }
    }
}
