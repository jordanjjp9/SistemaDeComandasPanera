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
        }

        private void frmBebidasC_Load(object sender, EventArgs e)
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
        }

        public string UltimaNotaSeleccionada { get; private set; } = string.Empty;

        public string TomarYLimpiarNota()
        {
            var n = UltimaNotaSeleccionada;
            UltimaNotaSeleccionada = string.Empty;
            return n;
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
                var m = Regex.Match(btn?.Name ?? "", @"\d+");
                if (m.Success) cod = m.Value;
            }
            if (string.IsNullOrWhiteSpace(cod)) return;

            // 1) leer cantidad desde frmMenuPrincipal (si está abierto)
            int cantidad = 1;
            var main = Application.OpenForms.OfType<frmMenuPrincipal>().FirstOrDefault();
            if (main != null) cantidad = main.CantidadActualPublic;

            using (var dlg = new frmNotasBebidas())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;

                // 2) pasar contexto (producto + cantidad)
                dlg.SetContexto(btn?.Text, cantidad);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    // Guarda nota para que frmMenuPrincipal la recoja (tú ya lo haces)
                    UltimaNotaSeleccionada = dlg.NotaSeleccionada;

                    // Avisar al padre para que agregue el producto (tu flujo actual)
                    ProductoSeleccionado?.Invoke(cod);
                }
            }
        }

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
