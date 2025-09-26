using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaDatos
{
    public class DAOPrecuenta
    {
        public DAOPrecuenta() { } // no necesitas _cs

        // Ejecuta el SP dbo.Pedido y devuelve su DataTable
        public DataTable ObtenerDesdePedido(string numPed)
        {
            using (var cn = Conexion.CrearConexion())
            using (var da = new SqlDataAdapter("dbo.Pedido", cn))
            {
                da.SelectCommand.CommandType = CommandType.StoredProcedure;
                da.SelectCommand.Parameters.Add("@cNumPed", SqlDbType.Char, 8).Value = numPed;
                var dt = new DataTable("Pedido");
                da.Fill(dt);
                return dt;
            }
        }

        // Opcional: trae las líneas con IGV = 0
        public DataTable ObtenerLineasIGV0(string numPed)
        {
            const string sql = @"
                SELECT d.num_item, d.can_pprd, d.pre_pprd, d.imp_tprd, d.pre_igv, d.imp_igv,
                       d.cdg_prod, p.des_prod
                FROM d_pedido d
                INNER JOIN m_produc p ON p.cdg_prod = d.cdg_prod
                WHERE d.num_ped = @numPed AND ISNULL(d.imp_igv,0) = 0;";

            using (var cn = Conexion.CrearConexion())
            using (var da = new SqlDataAdapter(sql, cn))
            {
                da.SelectCommand.Parameters.Add("@numPed", SqlDbType.Char, 8).Value = numPed;
                var dt = new DataTable("LineasIGV0");
                da.Fill(dt);
                return dt;
            }
        }

        // Lookup de nombre de ambiente (si solo tienes cdg_area)
        public string ObtenerNombreAmbiente(string cdgArea, string cdgTab = "ACJ")
        {
            if (string.IsNullOrWhiteSpace(cdgArea)) return cdgArea ?? "";
            using (var cn = Conexion.CrearConexion())
            using (var cmd = new SqlCommand(@"
                SELECT TOP 1 des_item
                FROM d_tablas WITH (NOLOCK)
                WHERE cdg_tab = @tab AND num_item = @area;", cn))
            {
                cmd.Parameters.AddWithValue("@tab", cdgTab);
                cmd.Parameters.AddWithValue("@area", cdgArea);
                cn.Open();
                var o = cmd.ExecuteScalar();
                return o == null || o == DBNull.Value ? cdgArea : o.ToString();
            }
        }
    }
}
