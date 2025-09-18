using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion.Helpers
{
    public static class LinesLocker
    {
        /// <summary>
        /// Deshabilita o habilita recursivamente todos los hijos del contenedor.
        /// Útil cuando quieres que las líneas ya guardadas no se puedan tocar.
        /// </summary>
        public static void SetChildrenEnabled(Control container, bool enabled)
        {
            if (container == null) return;
            foreach (Control c in container.Controls)
            {
                c.Enabled = enabled;
                if (c.HasChildren) SetChildrenEnabled(c, enabled);
            }
        }
    }
}
