using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CapaPresentacion.Controles;
using WF = System.Windows.Forms;

namespace CapaPresentacion.Helpers
{
    public interface ILineaSeleccionable
    {
        WF.Control View { get; }                 // control WinForms raíz del ítem
        void SetVisualSelected(bool selected);   // pinta/despinta
    }
    public class LineaSelection
    {
        private static ILineaSeleccionable _actual;
        public static ILineaSeleccionable Actual { get { return _actual; } }

        public static event EventHandler Changed;

        public static void Select(ILineaSeleccionable item, bool scrollIntoView = true)
        {
            if (object.ReferenceEquals(_actual, item)) return;

            if (_actual != null)
                _actual.SetVisualSelected(false);

            _actual = item;

            if (_actual != null)
            {
                _actual.SetVisualSelected(true);

                if (scrollIntoView)
                {
                    WF.Control c = _actual.View;
                    WF.ScrollableControl sc = GetScrollableAncestor(c);
                    if (sc != null) sc.ScrollControlIntoView(c);
                }
            }

            var h = Changed; if (h != null) h(null, EventArgs.Empty);
        }

        public static void Clear()
        {
            if (_actual != null)
            {
                _actual.SetVisualSelected(false);
                _actual = null;
                var h = Changed; if (h != null) h(null, EventArgs.Empty);
            }
        }

        private static WF.ScrollableControl GetScrollableAncestor(WF.Control c)
        {
            for (WF.Control p = (c == null ? null : c.Parent); p != null; p = p.Parent)
            {
                var s = p as WF.ScrollableControl;
                if (s != null && s.AutoScroll) return s;
            }
            return null;
        }
    }
}
