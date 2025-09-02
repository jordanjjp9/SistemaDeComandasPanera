using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion.Helpers
{
    public enum DragAxis { Horizontal, Vertical }
    public class DragScroller : IDisposable
    {
        private readonly ScrollableControl _host;
        private readonly DragAxis _axis;

        // drag
        private bool _dragging, _capturandoDrag, _cancelNextClick;
        private int _start, _origin, _lastPos, _lastDelta;
        private const int DragThreshold = 6;

        // inercia
        private readonly Timer _inertia = new Timer { Interval = 15 };
        private double _velocity = 0;
        private const double Decay = 0.90;

        public DragScroller(ScrollableControl host, DragAxis axis)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _axis = axis;

            _host.AutoScroll = true;
            OcultarScrollBars(_host);

            // host
            _host.MouseDown += Host_MouseDown;
            _host.MouseMove += Host_MouseMove;
            _host.MouseUp += Host_MouseUp;

            // 👉 ENGANCHAR TODO EL ÁRBOL (hijos, nietos, etc.)
            WireTree(_host);
            _host.ControlAdded += (_, e) => WireTree(e.Control);

            _inertia.Tick += Inertia_Tick;
        }

        public void Dispose()
        {
            _inertia.Stop();
            _inertia.Tick -= Inertia_Tick;
            _inertia.Dispose();
        }

        private static void OcultarScrollBars(ScrollableControl s)
        {
            try
            {
                s.HorizontalScroll.Enabled = false;
                s.HorizontalScroll.Visible = false;
                s.VerticalScroll.Enabled = false;
                s.VerticalScroll.Visible = false;
            }
            catch { }
        }

        // ========= WIRING PROFUNDO =========
        private void WireTree(Control root)
        {
            WireNode(root);
            foreach (Control c in root.Controls)
                WireTree(c);
        }

        private void WireNode(Control c)
        {
            // arrastre también desde el propio control
            c.MouseDown -= Host_MouseDown; c.MouseDown += Host_MouseDown;
            c.MouseMove -= Host_MouseMove; c.MouseMove += Host_MouseMove;
            c.MouseUp -= Host_MouseUp; c.MouseUp += Host_MouseUp;

            // evita que el foco provoque auto-scroll
            c.Enter -= (_, __) => _host.Focus();
            c.MouseDown -= (_, __) => _host.Focus();
            c.Enter += (_, __) => _host.Focus();
            c.MouseDown += (_, __) => _host.Focus();

            c.TabStop = false;

            // 👉 Si es un TextBox (o derivado) ponlo como solo lectura y sin atajos
            if (c is TextBoxBase tb)
            {
                tb.ReadOnly = true;
                tb.ShortcutsEnabled = false;
                tb.Cursor = Cursors.Hand;
            }

            // 👉 Si es un Guna2TextBox, intenta acceder a su TextBox interno
            if (c.GetType().Name.IndexOf("Guna2", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var inner = TryGetInnerTextBox(c);
                if (inner != null)
                {
                    inner.ReadOnly = true;
                    inner.ShortcutsEnabled = false;
                    inner.Cursor = Cursors.Hand;

                    inner.MouseDown -= Host_MouseDown; inner.MouseDown += Host_MouseDown;
                    inner.MouseMove -= Host_MouseMove; inner.MouseMove += Host_MouseMove;
                    inner.MouseUp -= Host_MouseUp; inner.MouseUp += Host_MouseUp;

                    inner.Enter -= (_, __) => _host.Focus();
                    inner.MouseDown -= (_, __) => _host.Focus();
                    inner.Enter += (_, __) => _host.Focus();
                    inner.MouseDown += (_, __) => _host.Focus();
                }
            }
        }

        private static TextBox TryGetInnerTextBox(Control guna2TextBox)
        {
            try
            {
                var prop = guna2TextBox.GetType().GetProperty(
                    "TextBox", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop?.GetValue(guna2TextBox, null) as TextBox;
            }
            catch { return null; }
        }

        // ========= DRAG =========
        private void Host_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            _dragging = true;
            _capturandoDrag = false;
            _cancelNextClick = false;

            _start = GetMouse();
            _origin = (_axis == DragAxis.Horizontal) ? -_host.AutoScrollPosition.X
                                                     : -_host.AutoScrollPosition.Y;

            _lastPos = _start;
            _lastDelta = 0;
            _velocity = 0;
            _inertia.Stop();
            _host.Cursor = Cursors.Hand;

            // quita selección de cualquier textbox
            ClearSelectionDeep(_host);
        }

        private void Host_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            int pos = GetMouse();
            int delta = pos - _start;

            if (!_capturandoDrag && Math.Abs(delta) > DragThreshold)
            {
                _capturandoDrag = true;
                _cancelNextClick = true;
                _host.Capture = true;
            }
            if (!_capturandoDrag) return;

            int target = _origin - delta;
            target = Math.Max(0, Math.Min(target, GetMaxScroll()));

            if (_axis == DragAxis.Horizontal)
                _host.AutoScrollPosition = new Point(target, -_host.AutoScrollPosition.Y);
            else
                _host.AutoScrollPosition = new Point(-_host.AutoScrollPosition.X, target);

            _lastDelta = pos - _lastPos;
            _lastPos = pos;

            // mientras arrastras, limpia selección
            ClearSelectionDeep(_host);
        }

        private void Host_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;

            if (_capturandoDrag)
            {
                _host.Capture = false;
                _velocity = _lastDelta;
                if (Math.Abs(_velocity) > 1) _inertia.Start();
            }

            _host.Cursor = Cursors.Default;
            _capturandoDrag = false;
        }

        // ========= INERCIA =========
        private void Inertia_Tick(object sender, EventArgs e)
        {
            int cur = (_axis == DragAxis.Horizontal) ? -_host.AutoScrollPosition.X
                                                     : -_host.AutoScrollPosition.Y;
            int other = (_axis == DragAxis.Horizontal) ? -_host.AutoScrollPosition.Y
                                                       : -_host.AutoScrollPosition.X;

            int target = cur - (int)Math.Round(_velocity);
            int max = GetMaxScroll();

            if (target < 0) { target = 0; _velocity = 0; }
            if (target > max) { target = max; _velocity = 0; }

            if (_axis == DragAxis.Horizontal)
                _host.AutoScrollPosition = new Point(target, other);
            else
                _host.AutoScrollPosition = new Point(other, target);

            _velocity *= Decay;
            if (Math.Abs(_velocity) < 0.5) _inertia.Stop();
        }

        // ========= HELPERS =========
        private int GetMouse()
        {
            var p = _host.PointToClient(Cursor.Position);
            return _axis == DragAxis.Horizontal ? p.X : p.Y;
        }

        private int GetMaxScroll()
        {
            if (_axis == DragAxis.Horizontal)
            {
                int overflow = _host.DisplayRectangle.Width - _host.ClientSize.Width;
                return Math.Max(0, overflow);
            }
            else
            {
                int overflow = _host.DisplayRectangle.Height - _host.ClientSize.Height;
                return Math.Max(0, overflow);
            }
        }

        private static void ClearSelectionDeep(Control root)
        {
            if (root is TextBoxBase tb)
                tb.Select(0, 0);

            foreach (Control c in root.Controls)
                ClearSelectionDeep(c);

            // también intenta limpiar el textbox interno de Guna2
            var inner = TryGetInnerTextBox(root);
            inner?.Select(0, 0);
        }

    }
}
