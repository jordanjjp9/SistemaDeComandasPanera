using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion.Helpers
{
    public class NoFocusScrollFlowLayoutPanel : FlowLayoutPanel
    {
        public NoFocusScrollFlowLayoutPanel()
        {
            DoubleBuffered = true;
            WrapContents = false; // Horizontal continuo
            AutoScroll = true;
        }


        // Anula el auto-scroll cuando un hijo toma foco (ScrollControlIntoView)
        protected override Point ScrollToControl(Control activeControl)
        {
            // Mantener exactamente la vista actual
            return this.DisplayRectangle.Location;
        }


        // Suavizado extra para evitar parpadeo en WinForms
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000; // WS_EX_COMPOSITED
                return cp;
            }
        }
    }
}
