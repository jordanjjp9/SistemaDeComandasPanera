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
    public partial class frmBebidasC : Form, ISelectorProducto
    {
        public event Action<string> ProductoSeleccionado;

        private bool _cancelNextClick = false;
        private bool _dragging = false;
        private int _dragStartX = 0;
        private int _originScrollX = 0;
        private int _lastX = 0;
        private int _lastDX = 0;

        private bool _capturandoDrag = false;
        private const int DragThreshold = 6;
        // private bool _cancelNextClick = false;

        // Inercia
        private System.Windows.Forms.Timer _inertiaTimer;
        private double _velocity = 0;       // px por tick (~15 ms)
        private const double Decay = 0.90;  // 0.85–0.95
        public frmBebidasC()
        {
            InitializeComponent();
            this.Load += frmBebidasC_Load;

            flpBebidasC.VerticalScroll.Visible = false;
            //flpAdicional.HorizontalScroll.Visible = true;

            //  _principal = principal;

            _inertiaTimer = new System.Windows.Forms.Timer { Interval = 15 };
            _inertiaTimer.Tick += _inertiaTimer_Tick;

            // Suscribir handlers (hazlo aquí o en el diseñador, pero no doble)
            flpBebidasC.MouseDown += flpBebidasC_MouseDown;
            flpBebidasC.MouseMove += flpBebidasC_MouseMove;
            flpBebidasC.MouseUp += flpBebidasC_MouseUp;

        }

        private void frmBebidasC_Load(object sender, EventArgs e)
        {
            foreach (var b in EnumerarBotones(this))
            {
                if (b.Name.StartsWith("btnProd", StringComparison.OrdinalIgnoreCase))
                {
                    // Toma el código del Name si no está en Tag
                    var cod = b.Tag as string;
                    if (string.IsNullOrWhiteSpace(cod))
                    {
                        var m = Regex.Match(b.Name, @"\d+"); // ej. btnProd0000000225 -> 0000000225
                        if (m.Success) cod = m.Value;
                    }

                    b.Tag = cod;              // guarda el código en Tag para reutilizar
                    b.Click -= BtnProducto_Click;
                    b.Click += BtnProducto_Click;
                }
            }
            flpBebidasC.AutoScroll = true; // NECESARIO para que calcule el área de scroll

            // (Opcional) suaviza el repintado
            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flpBebidasC, true);

            // 🔹 Ocultar barras de scroll, pero mantener AutoScroll funcionando
            flpBebidasC.HorizontalScroll.Enabled = false;
            flpBebidasC.HorizontalScroll.Visible = false;
            flpBebidasC.VerticalScroll.Enabled = false;
            flpBebidasC.VerticalScroll.Visible = false;

            // (Si ya tienes botones en el diseñador)
            foreach (Control c in flpBebidasC.Controls)
                WireChild(c);

            flpBebidasC.ControlAdded += (_, ev) => WireChild(ev.Control);

            flpBebidasC.AutoScrollPosition = new Point(1, 1);
            WireProductButtons(this);
        }
        private void WireProductButtons(Control root)
        {
            foreach (Control c in root.Controls)
            {
                if (c is Button || c.GetType().Name.Contains("Guna2Button"))
                {
                    if (c.Name.StartsWith("btnProd", StringComparison.OrdinalIgnoreCase))
                    {
                        var cod = c.Tag as string;
                        if (string.IsNullOrWhiteSpace(cod))
                        {
                            var m = Regex.Match(c.Name ?? "", @"\d+");
                            if (m.Success) cod = m.Value;
                        }
                        c.Tag = cod;
                        c.Click -= BtnProducto_Click;
                        c.Click += BtnProducto_Click;
                    }
                }
                WireProductButtons(c); // recursivo
            }
        }
        private static System.Collections.Generic.IEnumerable<Control> EnumerarBotones(Control raiz)
        {
            foreach (Control c in raiz.Controls)
            {
                // filtra por nombre de prefijo si quieres: if (c.Name.StartsWith("btnProd")) …
                if (c is Button || c.GetType().Name.Contains("Guna2Button"))
                    yield return c;

                foreach (var child in EnumerarBotones(c))
                    yield return child;
            }
        }

        //public string UltimaNotaSeleccionada { get; private set; } = string.Empty;

        //public string TomarYLimpiarNota()
        //{
        //    var n = UltimaNotaSeleccionada;
        //    UltimaNotaSeleccionada = string.Empty;
        //    return n;
        //}

        //private IEnumerable<Button> EnumerarBotones(Control raiz)
        //{
        //    foreach (Control c in raiz.Controls)
        //    {
        //        if (c is Button b) yield return b;
        //        if (c.HasChildren)
        //            foreach (var b2 in EnumerarBotones(c)) yield return b2;
        //    }
        //}

        private void BtnProducto_Click(object sender, EventArgs e)
        {
            var btn = sender as Control;
            var cod = btn?.Tag as string;

            if (string.IsNullOrWhiteSpace(cod))
            {
                var m = Regex.Match(btn?.Name ?? "", @"\d+");
                if (m.Success) cod = m.Value;
            }

            if (!string.IsNullOrWhiteSpace(cod))
                ProductoSeleccionado?.Invoke(cod);
        }
        private void WireChild(Control c)
        {
            // Drag en los hijos
            c.MouseDown -= flpBebidasC_MouseDown;
            c.MouseMove -= flpBebidasC_MouseMove;
            c.MouseUp -= flpBebidasC_MouseUp;
            c.MouseDown += flpBebidasC_MouseDown;
            c.MouseMove += flpBebidasC_MouseMove;
            c.MouseUp += flpBebidasC_MouseUp;


            // Evitar que el foco en el botón provoque auto-scroll del contenedor
            c.Enter -= Child_EnterRedirectFocus;
            c.MouseDown -= Child_MouseDownRedirectFocus;
            c.Enter += Child_EnterRedirectFocus;
            c.MouseDown += Child_MouseDownRedirectFocus;

            c.TabStop = false; // opcional
        }
        private void Child_EnterRedirectFocus(object sender, EventArgs e) => flpBebidasC.Focus();
        private void Child_MouseDownRedirectFocus(object sender, MouseEventArgs e) => flpBebidasC.Focus();

        private void flpBebidasC_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            _dragging = true;
            _capturandoDrag = false;      // aún no sabemos si será drag
            _cancelNextClick = false;     // por defecto, permitir click

            _dragStartX = MouseXEnFlp();
            _originScrollX = -flpBebidasC.AutoScrollPosition.X;
            _lastX = _dragStartX;
            _lastDX = 0;
            _velocity = 0;
            _inertiaTimer?.Stop();

            // ❌ NO capturar aquí
            // flpBotonera.Capture = true;
            Cursor = Cursors.Hand;
        }

        private void flpBebidasC_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            int x = MouseXEnFlp();
            int delta = x - _dragStartX;

            // Si ya es drag “real”, captura y anula el click
            if (!_capturandoDrag && Math.Abs(delta) > DragThreshold)
            {
                _capturandoDrag = true;
                _cancelNextClick = true;      // evitar Click en botones
                flpBebidasC.Capture = true;   // capturar recién aquí
            }

            // Si todavía no superó el umbral, no muevas nada (deja que sea click)
            if (!_capturandoDrag) return;

            int target = _originScrollX - delta;
            target = Math.Max(0, Math.Min(target, GetMaxScrollX()));

            int currentY = -flpBebidasC.AutoScrollPosition.Y;
            flpBebidasC.AutoScrollPosition = new Point(target, currentY);

            _lastDX = x - _lastX;
            _lastX = x;
        }

        private void flpBebidasC_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            _dragging = false;

            if (_capturandoDrag)
            {
                flpBebidasC.Capture = false; // liberar captura
                _velocity = _lastDX;
                if (Math.Abs(_velocity) > 1)
                    _inertiaTimer?.Start();
            }

            Cursor = Cursors.Default;
            _capturandoDrag = false;
        }

        private void _inertiaTimer_Tick(object sender, EventArgs e)
        {
            int currentX = -flpBebidasC.AutoScrollPosition.X;
            int currentY = -flpBebidasC.AutoScrollPosition.Y;

            int target = currentX - (int)Math.Round(_velocity);
            int max = GetMaxScrollX();

            if (target < 0) { target = 0; _velocity = 0; }
            if (target > max) { target = max; _velocity = 0; }

            flpBebidasC.AutoScrollPosition = new Point(target, currentY);

            _velocity *= Decay;
            if (Math.Abs(_velocity) < 0.5) _inertiaTimer?.Stop();
        }
        private int GetMaxScrollX()
        {
            int overflow = flpBebidasC.DisplayRectangle.Width - flpBebidasC.ClientSize.Width;
            return Math.Max(0, overflow);
        }

        private int MouseXEnFlp()
        {
            return flpBebidasC.PointToClient(Cursor.Position).X;
        }
    }
}
