using System;
using System.Collections.Generic;
using System.IO;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaPresentacion.Ambientes;
using CapaPresentacion.Botoneras;
using CapaPresentacion.Notas;
using CapaNegocio.MetaStore;

namespace CapaPresentacion
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            string root = ConfigurationManager.AppSettings["MetaRootPath"];
            if (string.IsNullOrWhiteSpace(root))
                root = @"\\192.168.1.4\DocumentosTemp";   // fallback

            try { Directory.CreateDirectory(root); } catch { }

            MetaStoreFactory.Configure(root);             // requiere ref a CapaNegocio

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmLogin());
        }
    }
}
