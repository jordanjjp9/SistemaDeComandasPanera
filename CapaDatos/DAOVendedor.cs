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
    public class DAOVendedor
    {
        private readonly string _cs;

        public DAOVendedor()
        {
            _cs = ConfigurationManager.ConnectionStrings["conexCholo"].ConnectionString;
        }

        /// <summary>
        /// Normaliza el código a 3 dígitos (ajusta si tu esquema usa otra longitud).
        /// </summary>
        private static string Norm(string codigo)
        {
            return (codigo ?? string.Empty).Trim().PadLeft(3, '0');
        }

        /// <summary>
        /// Obtiene un vendedor por código. Devuelve null si no existe.
        /// </summary>
        public ceVendedor ObtenerPorCodigo(string codigo)
        {
            const string sql = @"
            SELECT CDG_VEND,
                   DES_VEND,
                   CAST(ISNULL(SWT_VEND,0) AS INT) AS SWT_VEND
            FROM dbo.M_VENDED
            WHERE CDG_VEND = @cod;";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@cod", SqlDbType.VarChar, 3).Value = Norm(codigo);
                cn.Open();

                using (var dr = cmd.ExecuteReader())
                {
                    if (!dr.Read()) return null;

                    int swt = dr.GetInt32(2);      // <-- ahora siempre es int
                    bool activo = (swt == 1);

                    return new ceVendedor
                    {
                        Codigo = dr.GetString(0),
                        Nombre = dr.GetString(1),
                        Activo = activo
                    };
                }
            }
        }

        /// <summary>
        /// Devuelve el nombre si existe (y opcionalmente si está activo). Null si no existe.
        /// </summary>
        public string ObtenerNombreSiExiste(string codigo, bool soloActivos = true)
        {
            string sql = @"
            SELECT DES_VEND
            FROM dbo.M_VENDED
            WHERE CDG_VEND = @cod";

            if (soloActivos)
                sql += " AND SWT_VEND = 1";

            sql += ";";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@cod", SqlDbType.VarChar, 3).Value = Norm(codigo);

                cn.Open();
                object r = cmd.ExecuteScalar();
                return (r == null || r == DBNull.Value) ? null : (string)r;
            }
        }

        /// <summary>
        /// Indica si existe el código de vendedor (opcionalmente solo activos).
        /// </summary>
        public bool Existe(string codigo, bool soloActivos = true)
        {
            string sql = @"
            SELECT 1
            FROM dbo.M_VENDED
            WHERE CDG_VEND = @cod";

            if (soloActivos)
                sql += " AND SWT_VEND = 1";

            sql += ";";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@cod", SqlDbType.VarChar, 3).Value = Norm(codigo);

                cn.Open();
                object r = cmd.ExecuteScalar();
                return r != null;
            }
        }

        /// <summary>
        /// Lista vendedores con filtro opcional y estado (true=activos, false=inactivos, null=ambos).
        /// </summary>
        public List<ceVendedor> Listar(string filtro = null, bool? soloActivos = null)
        {
            var lista = new List<ceVendedor>();

            string sql = @"
            SELECT CDG_VEND, DES_VEND, SWT_VEND
            FROM dbo.M_VENDED
            WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(filtro))
                sql += " AND (CDG_VEND LIKE @f OR DES_VEND LIKE @f)";

            if (soloActivos == true)
                sql += " AND SWT_VEND = 1";
            else if (soloActivos == false)
                sql += " AND (SWT_VEND = 0 OR SWT_VEND IS NULL)";

            sql += " ORDER BY DES_VEND;";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (!string.IsNullOrWhiteSpace(filtro))
                    cmd.Parameters.Add("@f", SqlDbType.VarChar, 60).Value = "%" + filtro.Trim() + "%";

                cn.Open();
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        var v = new ceVendedor
                        {
                            Codigo = dr.GetString(0),
                            Nombre = dr.GetString(1),
                            Activo = !dr.IsDBNull(2) && dr.GetInt32(2) == 1
                        };
                        lista.Add(v);
                    }
                }
            }

            return lista;
        }

        /// <summary>
        /// Actualiza el estado (SWT_VEND) de un vendedor.
        /// </summary>
        public int ActualizarEstado(string codigo, bool activo)
        {
            const string sql = @"
            UPDATE dbo.M_VENDED
            SET SWT_VEND = @a
            WHERE CDG_VEND = @cod;";

            using (var cn = new SqlConnection(_cs))
            using (var cmd = new SqlCommand(sql, cn))
            {
                cmd.Parameters.Add("@a", SqlDbType.Int).Value = activo ? 1 : 0;
                cmd.Parameters.Add("@cod", SqlDbType.VarChar, 3).Value = Norm(codigo);

                cn.Open();
                return cmd.ExecuteNonQuery(); // filas afectadas
            }
        }
    }
}
