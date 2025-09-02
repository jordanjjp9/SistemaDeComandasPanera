using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion.Helpers
{
    public static class ControlExtensions
    {
        public static void DoubleBuffered(this Control control, bool enable)
        {
            if (control == null) return;

            // 1) DoubleBuffered (propiedad protegida)
            var prop = typeof(Control).GetProperty(
                "DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            prop?.SetValue(control, enable, null);

            // 2) SetStyle (método protegido)
            var setStyle = typeof(Control).GetMethod(
                "SetStyle",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            if (enable && setStyle != null)
            {
                var flags = ControlStyles.AllPaintingInWmPaint |
                            ControlStyles.UserPaint |
                            ControlStyles.OptimizedDoubleBuffer;

                setStyle.Invoke(control, new object[] { flags, true });
            }

            // 3) UpdateStyles (método protegido)
            var updateStyles = typeof(Control).GetMethod(
                "UpdateStyles",
                BindingFlags.Instance | BindingFlags.NonPublic
            );
            updateStyles?.Invoke(control, null);
        }
    }
}
