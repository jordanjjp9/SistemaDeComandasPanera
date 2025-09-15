using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaDatos
{
    internal static class DataReaderX
    {
        public static bool HasColumn(this IDataRecord r, string name)
        {
            for (int i = 0; i < r.FieldCount; i++)
                if (r.GetName(i).Equals(name, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static T Get<T>(this SqlDataReader r, string name, T def = default)
        {
            if (!((IDataRecord)r).HasColumn(name)) return def;
            int i = r.GetOrdinal(name);
            if (r.IsDBNull(i)) return def;
            object v = r.GetValue(i);
            return (T)Convert.ChangeType(v, typeof(T));
        }
    }
}
