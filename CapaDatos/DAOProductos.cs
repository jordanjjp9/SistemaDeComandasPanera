using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaEntidad;
using System.Configuration;

namespace CapaDatos
{
    public class DAOProductos
    {
        private readonly string _cs;


        public DAOProductos()
        {
            _cs = ConfigurationManager.ConnectionStrings["conexCholo"].ConnectionString;
        }

        private static decimal LeerDecimal(SqlDataReader dr, int ordinal)
        {
            return dr.IsDBNull(ordinal) ? 0m : dr.GetDecimal(ordinal);
        }

        private static string LeerString(SqlDataReader dr, int ordinal)
        {
            return dr.IsDBNull(ordinal) ? null : dr.GetString(ordinal).Trim();
        }

        /// <summary>
        /// Lista de productos con precio y unidad para la lista de ventas.
        /// Devuelve un DataTable con columnas: CDG_PROD, PRODUCTO, UNM, PRECIO, CANTIDAD, TOTAL.
        /// </summary>
        public DataTable ListarParaVenta(string lprc = "001")
        {
            string sql = @"
                SELECT
                    mpre.CDG_PROD   AS CDG_PROD,
                    LTRIM(RTRIM(prd.DES_PROD)) AS PRODUCTO,
                    unm.ABR_ITEM    AS UNM,
                    mpre.PRE_SOL    AS PRECIO
                FROM M_PRECIO mpre
                INNER JOIN M_PRODUC  prd ON prd.CDG_PROD = mpre.CDG_PROD AND mpre.CDG_LPRC = @lprc
                INNER JOIN D_TABLAS  unm ON unm.NUM_ITEM = prd.CDG_UMED AND unm.CDG_TAB   = 'UNM'
                WHERE mpre.CDG_LPRC = @lprc
                ORDER BY prd.DES_PROD;";

            using (var cn = new SqlConnection(_cs))
            using (var da = new SqlDataAdapter(sql, cn))
            {
                da.SelectCommand.Parameters.Add("@lprc", SqlDbType.VarChar, 10).Value = lprc;

                var dt = new DataTable();
                da.Fill(dt);

                // Columna CANTIDAD con valor por defecto = 1
                var cCant = new DataColumn("CANTIDAD", typeof(decimal))
                {
                    DefaultValue = 1m
                };
                dt.Columns.Add(cCant);

                // Columna TOTAL como expresión = CANTIDAD * PRECIO
                var cTot = new DataColumn("TOTAL", typeof(decimal))
                {
                    Expression = "CANTIDAD * PRECIO"
                };
                dt.Columns.Add(cTot);

                return dt;
            }
        }

        ////public List<ceProductos> ListarBasico(string lprc = "001")
        ////{
        ////    const string sql = @"
        ////    SELECT
        ////        mpre.CDG_PROD,
        ////        LTRIM(RTRIM(prd.DES_PROD)) AS DES_PROD,
        ////        mpre.PRE_SOL
        ////    FROM M_PRECIO mpre
        ////    INNER JOIN M_PRODUC prd ON prd.CDG_PROD = mpre.CDG_PROD
        ////    WHERE mpre.CDG_LPRC = @lprc
        ////    ORDER BY prd.DES_PROD;";

        ////    var lista = new List<ceProductos>();

        ////    using (var cn = new SqlConnection(_cs))
        ////    using (var cmd = new SqlCommand(sql, cn))
        ////    {
        ////        cmd.Parameters.Add("@lprc", SqlDbType.VarChar, 10).Value = (lprc ?? "001").Trim();
        ////        cn.Open();

        ////        using (var dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
        ////        {
        ////            while (dr.Read())
        ////            {
        ////                var prod = new ceProductos
        ////                {
        ////                    Codigo = dr.GetString(0).Trim(),
        ////                    Descripcion = dr.GetString(1).Trim(),
        ////                    PrecioUnitario = LeerDecimal(dr, 2),
        ////                    Activo = true // si tienes flag real en BD, mapéalo
        ////                };
        ////                lista.Add(prod);
        ////            }
        ////        }
        ////    }

        ////    return lista;
        ////}

        public List<ceProductos> ListarBasico(string lprc = "001")
        {
            const string sql = @"
            SELECT
                mpre.CDG_PROD,
                LTRIM(RTRIM(prd.DES_PROD)) AS DES_PROD,
                mpre.PRE_SOL,
                prd.CDG_TPRD                 -- << NUEVO
            FROM M_PRECIO mpre
            INNER JOIN M_PRODUC prd ON prd.CDG_PROD = mpre.CDG_PROD
            WHERE mpre.CDG_LPRC = @lprc
            ORDER BY prd.DES_PROD;";

            var lista = new List<ceProductos>();

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@lprc", SqlDbType.VarChar, 10).Value = (lprc ?? "001").Trim();
                cn.Open();

                using (var dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                {
                    int oCod = dr.GetOrdinal("CDG_PROD");
                    int oDesc = dr.GetOrdinal("DES_PROD");
                    int oPrec = dr.GetOrdinal("PRE_SOL");
                    int oTprd = dr.GetOrdinal("CDG_TPRD");     // << NUEVO

                    while (dr.Read())
                    {
                        var prod = new ceProductos
                        {
                            Codigo = LeerString(dr, oCod),
                            Descripcion = LeerString(dr, oDesc),
                            PrecioUnitario = LeerDecimal(dr, oPrec),
                            TipoProductoCodigo = LeerString(dr, oTprd),  // << NUEVO
                            Activo = true
                        };
                        lista.Add(prod);
                    }
                }
            }
            return lista;
        }

        /// <summary>
        /// Obtiene un producto por código (en una lista de precios).
        /// </summary>
        ////public ceProductos ObtenerPorCodigo(string codigo, string lprc = "001")
        ////{
        ////    const string sql = @"
        ////    SELECT TOP 1
        ////        mpre.CDG_PROD,
        ////        LTRIM(RTRIM(prd.DES_PROD)) AS DES_PROD,
        ////        mpre.PRE_SOL
        ////    FROM M_PRECIO mpre
        ////    INNER JOIN M_PRODUC prd ON prd.CDG_PROD = mpre.CDG_PROD
        ////    WHERE mpre.CDG_LPRC = @lprc AND mpre.CDG_PROD = @cod;";

        ////    if (string.IsNullOrWhiteSpace(codigo))
        ////        return null;

        ////    using (var cn = new SqlConnection(_cs))
        ////    using (var cmd = new SqlCommand(sql, cn))
        ////    {
        ////        cmd.Parameters.Add("@lprc", SqlDbType.VarChar, 10).Value = (lprc ?? "001").Trim();
        ////        cmd.Parameters.Add("@cod", SqlDbType.VarChar, 40).Value = codigo.Trim();

        ////        cn.Open();
        ////        using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection))
        ////        {
        ////            if (!dr.Read()) return null;

        ////            var prod = new ceProductos
        ////            {
        ////                Codigo = dr.GetString(0).Trim(),
        ////                Descripcion = dr.GetString(1).Trim(),
        ////                PrecioUnitario = LeerDecimal(dr, 2),
        ////                Activo = true
        ////            };
        ////            return prod;
        ////        }
        ////    }
        ////}

        // =======================
        // 3) ObtenerPorCodigo  (AÑADE prd.CDG_TPRD y mapéalo)
        // =======================
        public ceProductos ObtenerPorCodigo(string codigo, string lprc = "001")
        {
            const string sql = @"
            SELECT TOP 1
                mpre.CDG_PROD,
                LTRIM(RTRIM(prd.DES_PROD)) AS DES_PROD,
                mpre.PRE_SOL,
                prd.CDG_TPRD                -- << NUEVO
            FROM M_PRECIO mpre
            INNER JOIN M_PRODUC prd ON prd.CDG_PROD = mpre.CDG_PROD
            WHERE mpre.CDG_LPRC = @lprc AND mpre.CDG_PROD = @cod;";

            if (string.IsNullOrWhiteSpace(codigo))
                return null;

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@lprc", SqlDbType.VarChar, 10).Value = (lprc ?? "001").Trim();
                cmd.Parameters.Add("@cod", SqlDbType.VarChar, 40).Value = codigo.Trim();

                cn.Open();
                using (var dr = cmd.ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.CloseConnection))
                {
                    if (!dr.Read()) return null;

                    int oCod = dr.GetOrdinal("CDG_PROD");
                    int oDesc = dr.GetOrdinal("DES_PROD");
                    int oPrec = dr.GetOrdinal("PRE_SOL");
                    int oTprd = dr.GetOrdinal("CDG_TPRD");      // << NUEVO

                    var prod = new ceProductos
                    {
                        Codigo = LeerString(dr, oCod),
                        Descripcion = LeerString(dr, oDesc),
                        PrecioUnitario = LeerDecimal(dr, oPrec),
                        TipoProductoCodigo = LeerString(dr, oTprd),  // << NUEVO
                        Activo = true
                    };
                    return prod;
                }
            }
        }


        ////public List<ceProductos> ListarPorCategoria(string categoriaCod, string lprc = "001")
        ////{
        ////    string sql = @"
        ////        SELECT p.CDG_PROD,
        ////               LTRIM(RTRIM(p.DES_PROD)),
        ////               p.CDG_UMED,
        ////               unm.ABR_ITEM AS UMD,
        ////               pr.VAL_SOL,
        ////               pr.PRE_SOL,
        ////               pr.CDG_LPRC
        ////        FROM M_PRODUC p
        ////        JOIN M_PRECIO pr ON pr.CDG_PROD = p.CDG_PROD AND pr.CDG_LPRC=@lprc
        ////        JOIN D_TABLAS unm ON unm.CDG_TAB='UNM' AND unm.NUM_ITEM=p.CDG_UMED
        ////        WHERE p.CDG_FAM = @cat
        ////        ORDER BY p.DES_PROD;";

        ////    var lst = new List<ceProductos>();
        ////    using (var cn = new SqlConnection(_cs))
        ////    using (var cmd = new SqlCommand(sql, cn))
        ////    {
        ////        cmd.Parameters.Add("@cat", SqlDbType.VarChar, 10).Value = categoriaCod;
        ////        cmd.Parameters.Add("@lprc", SqlDbType.VarChar, 10).Value = lprc;

        ////        cn.Open();
        ////        using (var dr = cmd.ExecuteReader())
        ////        {
        ////            while (dr.Read())
        ////            {
        ////                lst.Add(new ceProductos
        ////                {
        ////                    Codigo = dr.GetString(0).Trim(),
        ////                    Descripcion = dr.GetString(1).Trim(),
        ////                    UnidadCodigo = dr.GetString(2).Trim(),
        ////                    UnidadDescripcion = dr.GetString(3).Trim(),
        ////                    ValorUnitario = dr.IsDBNull(4) ? 0m : dr.GetDecimal(4),
        ////                    PrecioUnitario = dr.IsDBNull(5) ? 0m : dr.GetDecimal(5),
        ////                    ListaPrecioCodigo = dr.GetString(6).Trim(),
        ////                    Activo = true
        ////                });
        ////            }
        ////        }
        ////    }
        ////    return lst;
        ////}

        // =======================
        // 4) ListarPorCategoria  (AÑADE p.CDG_TPRD y mapéalo)
        // =======================
        public List<ceProductos> ListarPorCategoria(string categoriaCod, string lprc = "001")
        {
            string sql = @"
                SELECT p.CDG_PROD,
                       LTRIM(RTRIM(p.DES_PROD)) AS DES_PROD,
                       p.CDG_UMED,
                       unm.ABR_ITEM AS UMD,
                       pr.VAL_SOL,
                       pr.PRE_SOL,
                       pr.CDG_LPRC,
                       p.CDG_TPRD                    -- << NUEVO
                FROM M_PRODUC p
                JOIN M_PRECIO pr ON pr.CDG_PROD = p.CDG_PROD AND pr.CDG_LPRC=@lprc
                JOIN D_TABLAS unm ON unm.CDG_TAB='UNM' AND unm.NUM_ITEM=p.CDG_UMED
                WHERE p.CDG_FAM = @cat
                ORDER BY p.DES_PROD;";

            var lst = new List<ceProductos>();
            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@cat", SqlDbType.VarChar, 10).Value = categoriaCod;
                cmd.Parameters.Add("@lprc", SqlDbType.VarChar, 10).Value = lprc;

                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    int oCod = dr.GetOrdinal("CDG_PROD");
                    int oDesc = dr.GetOrdinal("DES_PROD");
                    int oUmed = dr.GetOrdinal("CDG_UMED");
                    int oUmd = dr.GetOrdinal("UMD");
                    int oVal = dr.GetOrdinal("VAL_SOL");
                    int oPre = dr.GetOrdinal("PRE_SOL");
                    int oLprc = dr.GetOrdinal("CDG_LPRC");
                    int oTprd = dr.GetOrdinal("CDG_TPRD");      // << NUEVO

                    while (dr.Read())
                    {
                        lst.Add(new ceProductos
                        {
                            Codigo = LeerString(dr, oCod),
                            Descripcion = LeerString(dr, oDesc),
                            UnidadCodigo = LeerString(dr, oUmed),
                            UnidadDescripcion = LeerString(dr, oUmd),
                            ValorUnitario = LeerDecimal(dr, oVal),
                            PrecioUnitario = LeerDecimal(dr, oPre),
                            ListaPrecioCodigo = LeerString(dr, oLprc),
                            TipoProductoCodigo = LeerString(dr, oTprd),  // << NUEVO
                            Activo = true
                        });
                    }
                }
            }
            return lst;
        }

    }
}
