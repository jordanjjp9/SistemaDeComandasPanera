using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapaNegocio.MetaStore
{
    public class MetaStoreFactory
    {
        private static IMetaStore _current;

        // Llama esto una sola vez al iniciar la app
        public static void Configure(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                throw new ArgumentException("rootPath no puede ser vacío.");

            // Si estás usando CSV:
            _current = new CsvMetaStore(rootPath);

            // Si luego cambias a JSON, solo reemplaza por:
            // _current = new JsonMetaStore(rootPath);
        }

        public static IMetaStore Current
        {
            get
            {
                if (_current == null)
                    throw new InvalidOperationException("MetaStore no configurado. Llama a MetaStoreFactory.Configure(path) en el arranque.");
                return _current;
            }
        }
    }
}
