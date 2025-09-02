using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion.Helpers
{
    public class FlowLayoutPanelSinScroll : FlowLayoutPanel
    {
        public FlowLayoutPanelSinScroll()
        {
            this.AutoScroll = true;
            this.DoubleBuffered = true;
            this.ResizeRedraw = true;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCCALCSIZE = 0x0083;
            const int WM_NCPAINT = 0x0085;

            if (m.Msg == WM_NCCALCSIZE || m.Msg == WM_NCPAINT)
                return;

            base.WndProc(ref m);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            Ocultar();
        }
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            Ocultar();
        }
        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            Ocultar();
        }

        private void Ocultar()
        {
            this.HorizontalScroll.Enabled = false;
            this.HorizontalScroll.Visible = false;
            this.VerticalScroll.Enabled = false;
            this.VerticalScroll.Visible = false;
        }
    }
}
