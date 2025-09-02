using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaDatos
{
    public class Conexion
    {
        private static readonly string _cadena =
            ConfigurationManager.ConnectionStrings["conexCholo"].ConnectionString;

        public static SqlConnection CrearConexion()
        {
            return new SqlConnection(_cadena);
        }

        public static string Cadena => _cadena;

        public static bool ProbarConexion(out string error)
        {
            try
            {
                using (var cn = CrearConexion())
                {
                    cn.Open();
                    using (var cmd = new SqlCommand("SELECT 1", cn))
                        cmd.ExecuteScalar();
                }
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
