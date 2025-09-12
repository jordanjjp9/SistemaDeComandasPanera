using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using CapaEntidad;

namespace CapaDatos
{
    public class DAOVendedor
    {
        private readonly string _cs;

        public DAOVendedor()
        {
            _cs = ConfigurationManager.ConnectionStrings["conexCholo"].ConnectionString;
        }

        // ========================= Helpers =========================

        /// <summary>
        /// Normaliza CDG_VEND a 3 dígitos (ajusta si tu esquema usa otra longitud).
        /// </summary>
        private static string NormVend(string codigo)
            => (codigo ?? string.Empty).Trim().PadLeft(3, '0');

        /// <summary>
        /// Normaliza CDG_USR (trim simple).
        /// </summary>
        private static string NormUsr(string usr)
            => (usr ?? string.Empty).Trim();

        /// <summary>
        /// Arma un LIKE seguro para filtros.
        /// </summary>
        private static string Like(string s)
            => $"%{(s ?? string.Empty).Trim()}%";

        // ========================= POR CDG_VEND (EXISTENTE) =========================

        /// <summary>
        /// Obtiene un vendedor por CDG_VEND. Devuelve null si no existe.
        /// </summary>
        public ceVendedor ObtenerPorCodigo(string codigo)
        {
            const string sql = @"
                SELECT 
                    CDG_VEND,
                    DES_VEND,
                    CAST(ISNULL(SWT_VEND,0) AS INT) AS SWT_VEND,
                    CDG_USR
                FROM dbo.M_VENDED
                WHERE CDG_VEND = @cod;";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@cod", SqlDbType.VarChar, 3).Value = NormVend(codigo);
                cn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;

                    return new ceVendedor
                    {
                        Codigo = dr.GetString(0),                 // CDG_VEND
                        Nombre = dr.GetString(1),                 // DES_VEND
                        Activo = dr.GetInt32(2) == 1,             // SWT_VEND
                        CdgUsr = dr.IsDBNull(3) ? null : dr.GetString(3)
                    };
                }
            }
        }

        /// <summary>
        /// Devuelve el nombre si existe (y opcionalmente activo); null si no.
        /// </summary>
        public string ObtenerNombreSiExiste(string codigo, bool soloActivos = true)
        {
            var sb = new StringBuilder(@"
                SELECT DES_VEND
                FROM dbo.M_VENDED
                WHERE CDG_VEND = @cod");

            if (soloActivos) sb.Append(" AND ISNULL(SWT_VEND,0) = 1");
            sb.Append(";");

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sb.ToString(), cn))
            {
                cmd.Parameters.Add("@cod", SqlDbType.VarChar, 3).Value = NormVend(codigo);
                cn.Open();

                object r = cmd.ExecuteScalar();
                return (r == null || r == DBNull.Value) ? null : (string)r;
            }
        }

        /// <summary>
        /// Indica si existe el CDG_VEND (opcionalmente solo activos).
        /// </summary>
        public bool Existe(string codigo, bool soloActivos = true)
        {
            var sb = new StringBuilder(@"
                SELECT 1
                FROM dbo.M_VENDED
                WHERE CDG_VEND = @cod");

            if (soloActivos) sb.Append(" AND ISNULL(SWT_VEND,0) = 1");
            sb.Append(";");

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sb.ToString(), cn))
            {
                cmd.Parameters.Add("@cod", SqlDbType.VarChar, 3).Value = NormVend(codigo);
                cn.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        /// <summary>
        /// Lista vendedores con filtro (por código, nombre o usuario) y estado.
        /// </summary>
        public List<ceVendedor> Listar(string filtro = null, bool? soloActivos = null)
        {
            var lista = new List<ceVendedor>();

            var sb = new StringBuilder(@"
                SELECT
                    CDG_VEND,
                    DES_VEND,
                    CAST(ISNULL(SWT_VEND,0) AS INT) AS SWT_VEND,
                    CDG_USR
                FROM dbo.M_VENDED
                WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(filtro))
                sb.Append(" AND (CDG_VEND LIKE @f OR DES_VEND LIKE @f OR CDG_USR LIKE @f)");

            if (soloActivos == true)
                sb.Append(" AND ISNULL(SWT_VEND,0) = 1");
            else if (soloActivos == false)
                sb.Append(" AND ISNULL(SWT_VEND,0) = 0");

            sb.Append(" ORDER BY DES_VEND;");

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sb.ToString(), cn))
            {
                if (!string.IsNullOrWhiteSpace(filtro))
                    cmd.Parameters.Add("@f", SqlDbType.VarChar, 60).Value = Like(filtro);

                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        lista.Add(new ceVendedor
                        {
                            Codigo = dr.GetString(0),
                            Nombre = dr.GetString(1),
                            Activo = dr.GetInt32(2) == 1,
                            CdgUsr = dr.IsDBNull(3) ? null : dr.GetString(3)
                        });
                    }
                }
            }

            return lista;
        }

        /// <summary>
        /// Actualiza el estado activo/inactivo por CDG_VEND. Devuelve filas afectadas.
        /// </summary>
        public int ActualizarEstado(string codigo, bool activo)
        {
            const string sql = @"
                UPDATE dbo.M_VENDED
                SET SWT_VEND = @swt
                WHERE CDG_VEND = @cod;";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@swt", SqlDbType.Int).Value = activo ? 1 : 0;
                cmd.Parameters.Add("@cod", SqlDbType.VarChar, 3).Value = NormVend(codigo);

                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        // ========================= POR CDG_USR (NUEVO) =========================

        /// <summary>
        /// Indica si existe el CDG_USR (opcionalmente solo activos).
        /// </summary>
        public bool ExistePorUsr(string cdgUsr, bool soloActivos = true)
        {
            var sb = new StringBuilder(@"
                SELECT 1
                FROM dbo.M_VENDED
                WHERE CDG_USR = @usr");

            if (soloActivos) sb.Append(" AND ISNULL(SWT_VEND,0) = 1");
            sb.Append(";");

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sb.ToString(), cn))
            {
                cmd.Parameters.Add("@usr", SqlDbType.VarChar, 20).Value = NormUsr(cdgUsr);
                cn.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        /// <summary>
        /// Obtiene un vendedor por CDG_USR. Si soloActivos=true, filtra por SWT_VEND=1.
        /// </summary>
        public ceVendedor ObtenerPorUsr(string cdgUsr, bool soloActivos = true)
        {
            var sb = new StringBuilder(@"
                SELECT TOP 1
                    CDG_VEND,
                    DES_VEND,
                    CAST(ISNULL(SWT_VEND,0) AS INT) AS SWT_VEND,
                    CDG_USR
                FROM dbo.M_VENDED
                WHERE CDG_USR = @usr");

            if (soloActivos) sb.Append(" AND ISNULL(SWT_VEND,0) = 1");
            sb.Append(";");

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sb.ToString(), cn))
            {
                cmd.Parameters.Add("@usr", SqlDbType.VarChar, 20).Value = NormUsr(cdgUsr);
                cn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;

                    return new ceVendedor
                    {
                        Codigo = dr.GetString(0),                 // CDG_VEND
                        Nombre = dr.GetString(1),                 // DES_VEND
                        Activo = dr.GetInt32(2) == 1,             // SWT_VEND
                        CdgUsr = dr.IsDBNull(3) ? null : dr.GetString(3)
                    };
                }
            }
        }

        /// <summary>
        /// Login por CDG_USR sin PIN (tu esquema no tiene columna de credencial).
        /// Valida existencia + SWT_VEND=1.
        /// </summary>
        public bool ValidarLoginPorUsr(string cdgUsr)
        {
            const string sql = @"
                SELECT 1
                FROM dbo.M_VENDED
                WHERE CDG_USR = @usr
                  AND ISNULL(SWT_VEND,0) = 1;";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@usr", SqlDbType.VarChar, 20).Value = NormUsr(cdgUsr);
                cn.Open();
                return cmd.ExecuteScalar() != null;
            }
        }

        // ========================= UTILIDAD PARA ADMIN (OPCIONAL) =========================

        /// <summary>
        /// Devuelve una DataTable para el formulario administrador de usuarios.
        /// Columnas: CDG_VEND, DES_VEND, CDG_USR, SWT_VEND.
        /// </summary>
        public DataTable ListarTablaParaUsuarios(string filtro = null, bool? soloActivos = null)
        {
            var dt = new DataTable();

            var sb = new StringBuilder(@"
                SELECT
                    CDG_VEND,
                    DES_VEND,
                    CDG_USR,
                    CAST(ISNULL(SWT_VEND,0) AS INT) AS SWT_VEND
                FROM dbo.M_VENDED
                WHERE 1=1");

            if (!string.IsNullOrWhiteSpace(filtro))
                sb.Append(" AND (CDG_VEND LIKE @f OR DES_VEND LIKE @f OR CDG_USR LIKE @f)");

            if (soloActivos == true)
                sb.Append(" AND ISNULL(SWT_VEND,0) = 1");
            else if (soloActivos == false)
                sb.Append(" AND ISNULL(SWT_VEND,0) = 0");

            sb.Append(" ORDER BY DES_VEND;");

            using (var cn = new SqlConnection(_cs))
            using (var da = new SqlDataAdapter(sb.ToString(), cn))
            {
                if (!string.IsNullOrWhiteSpace(filtro))
                {
                    da.SelectCommand.Parameters.Add("@f", SqlDbType.VarChar, 60).Value = Like(filtro);
                }

                da.Fill(dt);
            }

            return dt;
        }

        public int ActualizarUsrPorVend(string cdgVend, string nuevoUsr)
        {
            const string sql = @"
                UPDATE dbo.M_VENDED
                   SET CDG_USR = @usr
                 WHERE CDG_VEND = @vend;";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                // CDG_VEND en tu esquema es de 3 dígitos: normalizamos
                cmd.Parameters.Add("@vend", SqlDbType.VarChar, 3).Value = (cdgVend ?? "").Trim().PadLeft(3, '0');

                // CDG_USR: 4 dígitos numéricos (según tu validación). Aquí solo lo seteamos.
                cmd.Parameters.Add("@usr", SqlDbType.VarChar, 20).Value = (nuevoUsr ?? "").Trim();

                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }
    }
}
