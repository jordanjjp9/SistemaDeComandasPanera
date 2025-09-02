using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaPresentacion.Botoneras;
using CapaPresentacion.Helpers;

namespace CapaPresentacion
{
    public partial class frmBotoneraPrincipal : Form
    {
        private readonly frmMenuPrincipal _principal;


        // Arrastre
        private bool _cancelNextClick = false;
        private bool _dragging = false;
        private int _dragStartX = 0;
        private int _originScrollX = 0;
        private int _lastX = 0;
        private int _lastDX = 0;
        private bool _capturandoDrag = false;
        private const int DragThreshold = 6; // px


        // Inercia
        private System.Windows.Forms.Timer _inertiaTimer;
        private double _velocity = 0; // px por tick (~15 ms)
        private const double Decay = 0.90; // 0.85–0.95


        public frmBotoneraPrincipal() : this(null) { }
        public frmBotoneraPrincipal(frmMenuPrincipal principal)
        {
            InitializeComponent();
            _principal = principal;

            _inertiaTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _inertiaTimer.Tick += _inertiaTimer_Tick;

            // Drag sobre el contenedor y (luego) se cablean niños en Load
            flpBotoneraSup.MouseDown += flpBotoneraSup_MouseDown;
            flpBotoneraSup.MouseMove += flpBotoneraSup_MouseMove;
            flpBotoneraSup.MouseUp += flpBotoneraSup_MouseUp;

            this.Load += frmBotoneraPrincipal_Load;
        }

        private void frmBotoneraPrincipal_Load(object sender, EventArgs e)
        {
            flpBotoneraSup.FlowDirection = FlowDirection.LeftToRight;
            flpBotoneraSup.WrapContents = false;
            flpBotoneraSup.AutoScroll = true;
            flpBotoneraSup.HorizontalScroll.Enabled = false;
            flpBotoneraSup.HorizontalScroll.Visible = false;
            flpBotoneraSup.VerticalScroll.Enabled = false;
            flpBotoneraSup.VerticalScroll.Visible = false;

            foreach (Control c in flpBotoneraSup.Controls)
                WireChild(c);

            flpBotoneraSup.ControlAdded += (_, ev) => WireChild(ev.Control);
        }

        private void WireChild(Control c)
        {
            // Drag en los hijos
            c.MouseDown -= flpBotoneraSup_MouseDown;
            c.MouseMove -= flpBotoneraSup_MouseMove;
            c.MouseUp -= flpBotoneraSup_MouseUp;
            c.MouseDown += flpBotoneraSup_MouseDown;
            c.MouseMove += flpBotoneraSup_MouseMove;
            c.MouseUp += flpBotoneraSup_MouseUp;


            // Evitar que el foco en el botón provoque auto-scroll del contenedor
            c.Enter -= Child_EnterRedirectFocus;
            c.MouseDown -= Child_MouseDownRedirectFocus;
            c.Enter += Child_EnterRedirectFocus;
            c.MouseDown += Child_MouseDownRedirectFocus;

            c.TabStop = false; // opcional
        }


        private void Child_EnterRedirectFocus(object sender, EventArgs e) => flpBotoneraSup.Focus();
        private void Child_MouseDownRedirectFocus(object sender, MouseEventArgs e) => flpBotoneraSup.Focus();

        private void flpBotoneraSup_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            this.ActiveControl = null;   // evita que el botón retenga foco
            _dragging = true;
            _capturandoDrag = false;
            _cancelNextClick = false;

            _dragStartX = MouseXEnFlp();
            _originScrollX = -flpBotoneraSup.AutoScrollPosition.X;
            _lastX = _dragStartX;
            _lastDX = 0;
            _velocity = 0;
            _inertiaTimer?.Stop();
            Cursor = Cursors.Hand;
        }

        private void flpBotoneraSup_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            int x = MouseXEnFlp();
            int delta = x - _dragStartX;

            if (!_capturandoDrag && Math.Abs(delta) > DragThreshold)
            {
                _capturandoDrag = true;
                _cancelNextClick = true;
                flpBotoneraSup.Capture = true;
            }

            if (!_capturandoDrag) return;

            int target = _originScrollX - delta;
            target = Math.Max(0, Math.Min(target, GetMaxScrollX()));

            int currentY = -flpBotoneraSup.AutoScrollPosition.Y;
            flpBotoneraSup.AutoScrollPosition = new Point(target, currentY);

            _lastDX = x - _lastX;
            _lastX = x;
        }

        private void flpBotoneraSup_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;

            if (_capturandoDrag)
            {
                flpBotoneraSup.Capture = false;
                _velocity = _lastDX;
                if (Math.Abs(_velocity) > 1)
                    _inertiaTimer?.Start();
            }

            Cursor = Cursors.Default;
            _capturandoDrag = false;
        }

        private void _inertiaTimer_Tick(object sender, EventArgs e)
        {
            int currentX = -flpBotoneraSup.AutoScrollPosition.X;
            int currentY = -flpBotoneraSup.AutoScrollPosition.Y;

            int target = currentX - (int)Math.Round(_velocity);
            int max = GetMaxScrollX();

            if (target < 0) { target = 0; _velocity = 0; }
            if (target > max) { target = max; _velocity = 0; }

            flpBotoneraSup.AutoScrollPosition = new Point(target, currentY);

            _velocity *= Decay;
            if (Math.Abs(_velocity) < 0.5) _inertiaTimer?.Stop();
        }


        private int GetMaxScrollX()
        {
            int overflow = flpBotoneraSup.DisplayRectangle.Width - flpBotoneraSup.ClientSize.Width;
            return Math.Max(0, overflow);
        }

        private int MouseXEnFlp() => flpBotoneraSup.PointToClient(Cursor.Position).X;

        private bool ConsumeIfDrag()
        {
            if (_cancelNextClick) { _cancelNextClick = false; return true; }
            return false;
        }


        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (_inertiaTimer != null)
                {
                    _inertiaTimer.Stop();
                    _inertiaTimer.Tick -= _inertiaTimer_Tick;
                    _inertiaTimer.Dispose();
                    _inertiaTimer = null;
                }
            }
            catch { }
            base.OnFormClosed(e);
        }

        private void btnAdicionales012_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmAdicional());

            //if (ConsumeIfDrag()) return;
        }

        private void btnBebidasC029_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmBebidasC());
            //  if (ConsumeIfDrag()) return;
        }

        private void btnBebidasF035_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmBebidasF());
            //    if (ConsumeIfDrag()) return;
        }

        private void btnBocaditos050_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmBocaditos());
            // if (ConsumeIfDrag()) return;
        }

        private void btnDesayunos039_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmDesayunos());
            //if (ConsumeIfDrag()) return;
        }

        private void btnEnsaladas042_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmEnsaladas());
            //  if (ConsumeIfDrag()) return;
        }
        private void btnHamyPiz044_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmHamburguesasyPizza());
            //   if (ConsumeIfDrag()) return;
        }
        private void btnJugos048_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmJugos());
            //if (ConsumeIfDrag()) return;
        }

        private void btnWaffles051_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmWaffles());
            if (ConsumeIfDrag()) return;
        }

        private void btnChicharron037_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmChicharron());
            if (ConsumeIfDrag()) return;
        }

        private void btnTortasEnteras_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmTortasEnteras());
            if (ConsumeIfDrag()) return;
        }

        private void btnFiambres060_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmFiambres());
        }

        private void btnHelad040_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmHeladosyMilkshake());
        }

        private void btnSand008_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmSandwich());
        }

        private void btnPorcion046_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmPorciones());
        }

        private void btnPastas031_Click(object sender, EventArgs e)
        {
            if (_cancelNextClick) { _cancelNextClick = false; return; }  // venías arrastrando
            _principal?.MostrarEnCentral(new CapaPresentacion.Botoneras.frmPastas());
        }
    }
}
