using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaPresentacion.Helpers;

namespace CapaPresentacion.Botoneras
{
    public partial class frmPastas : Form, ISelectorProducto
    {
        public event Action<string> ProductoSeleccionado;

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

        private readonly frmMenuPrincipal _principal;
        public frmPastas()
        {
            InitializeComponent();

            this.Load += frmPastas_Load;

            flpPastas.VerticalScroll.Visible = false;
            //flpAdicional.HorizontalScroll.Visible = true;

            //  _principal = principal;

            _inertiaTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _inertiaTimer.Tick += _inertiaTimer_Tick;

            // Suscribir handlers (hazlo aquí o en el diseñador, pero no doble)
            flpPastas.MouseDown += flpPastas_MouseDown;
            flpPastas.MouseMove += flpPastas_MouseMove;
            flpPastas.MouseUp += flpPastas_MouseUp;
        }

        private void frmPastas_Load(object sender, EventArgs e)
        {
            foreach (var b in EnumerarBotones(this))
            {
                if (b.Name.StartsWith("btnProd", StringComparison.OrdinalIgnoreCase))
                {
                    // Obtiene el código desde el Name o desde el Tag si ya lo pusiste
                    var cod = b.Tag as string;
                    if (string.IsNullOrWhiteSpace(cod))
                    {
                        var m = Regex.Match(b.Name, @"\d+"); // ejemplo: btnProd0000000225 -> 0000000225
                        if (m.Success) cod = m.Value;
                    }

                    b.Tag = cod; // guarda en Tag
                    b.Click -= BtnProducto_Click;
                    b.Click += BtnProducto_Click;
                }
            }
            flpPastas.AutoScroll = true; // NECESARIO para que calcule el área de scroll

            // (Opcional) suaviza el repintado
            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flpPastas, true);

            // 🔹 Ocultar barras de scroll, pero mantener AutoScroll funcionando
            flpPastas.HorizontalScroll.Enabled = false;
            flpPastas.HorizontalScroll.Visible = false;
            flpPastas.VerticalScroll.Enabled = false;
            flpPastas.VerticalScroll.Visible = false;

            // (Si ya tienes botones en el diseñador)
            foreach (Control c in flpPastas.Controls)
                WireChild(c);

            flpPastas.ControlAdded += (_, ev) => WireChild(ev.Control);

            flpPastas.AutoScrollPosition = new Point(1, 1);
        }

        private IEnumerable<Button> EnumerarBotones(Control raiz)
        {
            foreach (Control c in raiz.Controls)
            {
                if (c is Button b) yield return b;
                if (c.HasChildren)
                    foreach (var b2 in EnumerarBotones(c)) yield return b2;
            }
        }

        private void BtnProducto_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var cod = btn?.Tag as string;

            if (string.IsNullOrWhiteSpace(cod))
            {
                // fallback por si no hubiera Tag
                var m = Regex.Match(btn?.Name ?? "", @"\d+");
                if (m.Success) cod = m.Value;
            }

            if (!string.IsNullOrWhiteSpace(cod))
                ProductoSeleccionado?.Invoke(cod); // ← avisa al padre
        }

        private void WireChild(Control c)
        {
            // Drag en los hijos
            c.MouseDown -= flpPastas_MouseDown;
            c.MouseMove -= flpPastas_MouseMove;
            c.MouseUp -= flpPastas_MouseUp;
            c.MouseDown += flpPastas_MouseDown;
            c.MouseMove += flpPastas_MouseMove;
            c.MouseUp += flpPastas_MouseUp;


            // Evitar que el foco en el botón provoque auto-scroll del contenedor
            c.Enter -= Child_EnterRedirectFocus;
            c.MouseDown -= Child_MouseDownRedirectFocus;
            c.Enter += Child_EnterRedirectFocus;
            c.MouseDown += Child_MouseDownRedirectFocus;

            c.TabStop = false; // opcional
        }

        private void Child_EnterRedirectFocus(object sender, EventArgs e) => flpPastas.Focus();
        private void Child_MouseDownRedirectFocus(object sender, MouseEventArgs e) => flpPastas.Focus();

        private void flpPastas_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            this.ActiveControl = null;   // evita que el botón retenga foco
            _dragging = true;
            _capturandoDrag = false;
            _cancelNextClick = false;

            _dragStartX = MouseXEnFlp();
            _originScrollX = -flpPastas.AutoScrollPosition.X;
            _lastX = _dragStartX;
            _lastDX = 0;
            _velocity = 0;
            _inertiaTimer?.Stop();
            Cursor = Cursors.Hand;
        }

        private void flpPastas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            int x = MouseXEnFlp();
            int delta = x - _dragStartX;

            if (!_capturandoDrag && Math.Abs(delta) > DragThreshold)
            {
                _capturandoDrag = true;
                _cancelNextClick = true;
                flpPastas.Capture = true;
            }

            if (!_capturandoDrag) return;

            int target = _originScrollX - delta;
            target = Math.Max(0, Math.Min(target, GetMaxScrollX()));

            int currentY = -flpPastas.AutoScrollPosition.Y;
            flpPastas.AutoScrollPosition = new Point(target, currentY);

            _lastDX = x - _lastX;
            _lastX = x;
        }

        private void flpPastas_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;

            if (_capturandoDrag)
            {
                flpPastas.Capture = false;
                _velocity = _lastDX;
                if (Math.Abs(_velocity) > 1)
                    _inertiaTimer?.Start();
            }

            Cursor = Cursors.Default;
            _capturandoDrag = false;
        }
        private void _inertiaTimer_Tick(object sender, EventArgs e)
        {
            int currentX = -flpPastas.AutoScrollPosition.X;
            int currentY = -flpPastas.AutoScrollPosition.Y;

            int target = currentX - (int)Math.Round(_velocity);
            int max = GetMaxScrollX();

            if (target < 0) { target = 0; _velocity = 0; }
            if (target > max) { target = max; _velocity = 0; }

            flpPastas.AutoScrollPosition = new Point(target, currentY);

            _velocity *= Decay;
            if (Math.Abs(_velocity) < 0.5) _inertiaTimer?.Stop();
        }
        private int GetMaxScrollX()
        {
            int overflow = flpPastas.DisplayRectangle.Width - flpPastas.ClientSize.Width;
            return Math.Max(0, overflow);
        }

        private int MouseXEnFlp() => flpPastas.PointToClient(Cursor.Position).X;

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
    }
}
