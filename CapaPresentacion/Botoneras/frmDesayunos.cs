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
    public partial class frmDesayunos : Form, ISelectorProducto
    {
        public event Action<string> ProductoSeleccionado;

        // Drag horizontal con inercia (para el FlowLayoutPanel de los botones)
        private bool _cancelNextClick = false;
        private bool _dragging = false;
        private int _dragStartX = 0;
        private int _originScrollX = 0;
        private int _lastX = 0;
        private int _lastDX = 0;
        private bool _capturandoDrag = false;
        private const int DragThreshold = 6;

        private Timer _inertiaTimer;
        private double _velocity = 0;       // px por tick (~15 ms)
        private const double Decay = 0.90;  // 0.85–0.95

        public frmDesayunos()
        {
            InitializeComponent();
            this.Load += frmDesayunos_Load;

            // Timer de inercia
            _inertiaTimer = new Timer { Interval = 15 };
            _inertiaTimer.Tick += _inertiaTimer_Tick;

            // Handlers de drag directamente en el contenedor
            flpDesayuno.MouseDown += flpDesayuno_MouseDown;
            flpDesayuno.MouseMove += flpDesayuno_MouseMove;
            flpDesayuno.MouseUp += flpDesayuno_MouseUp;
        }

        private void frmDesayunos_Load(object sender, EventArgs e)
        {
            // AutoScroll requerido para que funcione el desplazamiento
            flpDesayuno.AutoScroll = true;

            // Oculta barras, pero mantiene el scroll programático
            try
            {
                flpDesayuno.HorizontalScroll.Enabled = false;
                flpDesayuno.HorizontalScroll.Visible = false;
                flpDesayuno.VerticalScroll.Enabled = false;
                flpDesayuno.VerticalScroll.Visible = false;
            }
            catch { }

            // Suaviza el repintado (doble buffer por reflexión)
            try
            {
                var pi = typeof(Panel).GetProperty("DoubleBuffered",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pi != null) pi.SetValue(flpDesayuno, true, null);
            }
            catch { }

            // Cablea todo el árbol de controles:
            WireProductoButtons(this);

            // Si agregas controles dinámicamente, vuelve a cablearlos
            this.ControlAdded += delegate (object _, ControlEventArgs ev)
            {
                WireProductoButtons(ev.Control);
            };

            // Posición inicial
            flpDesayuno.AutoScrollPosition = new Point(1, 1);
        }

        private void WireProductoButtons(Control root)
        {
            if (root == null) return;

            // Si es un “botón de producto” por convención de nombre
            if (!string.IsNullOrEmpty(root.Name) &&
                root.Name.StartsWith("btnProd", StringComparison.OrdinalIgnoreCase))
            {
                // Obtén/actualiza el código (10 dígitos normalmente) y guárdalo en Tag
                string cod = root.Tag as string;
                if (string.IsNullOrWhiteSpace(cod))
                {
                    Match m = Regex.Match(root.Name, @"\d+");
                    if (m.Success) cod = m.Value;
                }
                root.Tag = cod;

                // Click del control (funciona con Button, Guna2Button, etc.)
                root.Click -= BtnProducto_Click;
                root.Click += BtnProducto_Click;

                // Arrastre sobre el propio control también
                root.MouseDown -= flpDesayuno_MouseDown;
                root.MouseMove -= flpDesayuno_MouseMove;
                root.MouseUp -= flpDesayuno_MouseUp;
                root.MouseDown += flpDesayuno_MouseDown;
                root.MouseMove += flpDesayuno_MouseMove;
                root.MouseUp += flpDesayuno_MouseUp;
            }

            // Recorre hijos
            foreach (Control c in root.Controls)
                WireProductoButtons(c);
        }

        ////private IEnumerable<Button> EnumerarBotones(Control raiz)
        ////{
        ////    foreach (Control c in raiz.Controls)
        ////    {
        ////        if (c is Button b) yield return b;
        ////        if (c.HasChildren)
        ////        {
        ////            foreach (var bb in EnumerarBotones(c))
        ////                yield return bb;
        ////        }
        ////    }
        ////}
        private void BtnProducto_Click(object sender, EventArgs e)
        {
            // Si venías arrastrando, consume el click
            if (_cancelNextClick) { _cancelNextClick = false; return; }

            Control ctrl = sender as Control;
            string cod = ctrl != null ? ctrl.Tag as string : null;

            if (string.IsNullOrWhiteSpace(cod) && ctrl != null)
            {
                Match m = Regex.Match(ctrl.Name ?? "", @"\d+");
                if (m.Success) cod = m.Value;
            }

            if (!string.IsNullOrWhiteSpace(cod) && ProductoSeleccionado != null)
                ProductoSeleccionado(cod);
        }
        private void flpDesayuno_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            _dragging = true;
            _capturandoDrag = false;   // aún no superó el umbral
            _cancelNextClick = false;  // de momento permitimos click

            _dragStartX = MouseXEnFlp();
            _originScrollX = -flpDesayuno.AutoScrollPosition.X;
            _lastX = _dragStartX;
            _lastDX = 0;
            _velocity = 0;
            if (_inertiaTimer != null) _inertiaTimer.Stop();

            Cursor = Cursors.Hand;
        }
        private void flpDesayuno_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            int x = MouseXEnFlp();
            int delta = x - _dragStartX;

            if (!_capturandoDrag && Math.Abs(delta) > DragThreshold)
            {
                _capturandoDrag = true;
                _cancelNextClick = true;   // evita click en el botón tras arrastrar
                flpDesayuno.Capture = true;
            }
            if (!_capturandoDrag) return;

            int target = _originScrollX - delta;
            target = Math.Max(0, Math.Min(target, GetMaxScrollX()));

            int currentY = -flpDesayuno.AutoScrollPosition.Y;
            flpDesayuno.AutoScrollPosition = new Point(target, currentY);

            _lastDX = x - _lastX;
            _lastX = x;
        }
        private void flpDesayuno_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            _dragging = false;
            if (_capturandoDrag)
            {
                flpDesayuno.Capture = false;
                _velocity = _lastDX;
                if (Math.Abs(_velocity) > 1 && _inertiaTimer != null)
                    _inertiaTimer.Start();
            }

            Cursor = Cursors.Default;
            _capturandoDrag = false;
        }
        private void _inertiaTimer_Tick(object sender, EventArgs e)
        {
            int currentX = -flpDesayuno.AutoScrollPosition.X;
            int currentY = -flpDesayuno.AutoScrollPosition.Y;

            int target = currentX - (int)Math.Round(_velocity);
            int max = GetMaxScrollX();

            if (target < 0) { target = 0; _velocity = 0; }
            if (target > max) { target = max; _velocity = 0; }

            flpDesayuno.AutoScrollPosition = new Point(target, currentY);

            _velocity *= Decay;
            if (Math.Abs(_velocity) < 0.5 && _inertiaTimer != null)
                _inertiaTimer.Stop();
        }
        private int GetMaxScrollX()
        {
            int overflow = flpDesayuno.DisplayRectangle.Width - flpDesayuno.ClientSize.Width;
            return Math.Max(0, overflow);
        }

        ////private int MouseXEnFlp()
        ////{
        ////    return flpDesayuno.PointToClient(Cursor.Position).X;
        ////}
        private int MouseXEnFlp()
        {
            Point p = flpDesayuno.PointToClient(Cursor.Position);
            return p.X;
        }
    }
}
