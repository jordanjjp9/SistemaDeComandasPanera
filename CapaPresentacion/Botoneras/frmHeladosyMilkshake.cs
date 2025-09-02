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
    public partial class frmHeladosyMilkshake : Form, ISelectorProducto
    {
        public event Action<string> ProductoSeleccionado;

        // ==== NUEVO: nombres exactos de los dos botones que deben abrir el diálogo ====
        private const string BtnHelado2Bolas = "btnProd0000000583";
        private const string BtnHelado1Bola = "btnProd0000000584";

        // Arrastre / inercia (igual que lo tenías)
        private bool _cancelNextClick = false;
        private bool _dragging = false;
        private int _dragStartX = 0;
        private int _originScrollX = 0;
        private int _lastX = 0;
        private int _lastDX = 0;
        private bool _capturandoDrag = false;
        private const int DragThreshold = 6;
        private System.Windows.Forms.Timer _inertiaTimer;
        private double _velocity = 0;
        private const double Decay = 0.90;

        public frmHeladosyMilkshake()
        {
            InitializeComponent();


            this.Load += frmHeladosyMilkshake_Load;

            flpHelymilk.VerticalScroll.Visible = false;

            _inertiaTimer = new System.Windows.Forms.Timer();
            _inertiaTimer.Interval = 15;
            _inertiaTimer.Tick += _inertiaTimer_Tick;

            flpHelymilk.MouseDown += flpHelymilk_MouseDown;
            flpHelymilk.MouseMove += flpHelymilk_MouseMove;
            flpHelymilk.MouseUp += flpHelymilk_MouseUp;
        }

        private void frmHeladosyMilkshake_Load(object sender, EventArgs e)
        {
            foreach (Control b in EnumerarControles(this))
            {
                if (b.Name.StartsWith("btnProd", StringComparison.OrdinalIgnoreCase))
                {
                    // Código desde Tag o desde el Name (ej: btnProd0000000225)
                    string cod = b.Tag as string;
                    if (string.IsNullOrWhiteSpace(cod))
                    {
                        Match m = Regex.Match(b.Name ?? "", @"\d+");
                        if (m.Success) cod = m.Value;
                    }
                    b.Tag = cod;

                    b.Click -= BtnProducto_Click;
                    b.Click += BtnProducto_Click;
                }
            }

            flpHelymilk.AutoScroll = true;

            typeof(Panel).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(flpHelymilk, true);

            flpHelymilk.HorizontalScroll.Enabled = false;
            flpHelymilk.HorizontalScroll.Visible = false;
            flpHelymilk.VerticalScroll.Enabled = false;
            flpHelymilk.VerticalScroll.Visible = false;

            foreach (Control c in flpHelymilk.Controls)
            {
                if (c.Name.StartsWith("btnProd", StringComparison.OrdinalIgnoreCase))
                {
                    c.MouseDown += flpHelymilk_MouseDown;
                    c.MouseMove += flpHelymilk_MouseMove;
                    c.MouseUp += flpHelymilk_MouseUp;
                }
            }

            flpHelymilk.AutoScrollPosition = new Point(1, 1);
        }

        private static bool EsHeladoConNota(Control btn)
        {
            if (btn == null) return false;

            // por NOMBRE exacto (lo que pediste)
            if (btn.Name.Equals(BtnHelado1Bola, StringComparison.OrdinalIgnoreCase)) return true;
            if (btn.Name.Equals(BtnHelado2Bolas, StringComparison.OrdinalIgnoreCase)) return true;

            // opcional por TEXTO (por si cambias los nombres en el futuro)
            string t = (btn.Text ?? "").Trim();
            if (t.Equals("HELADO DE 1 BOLA", StringComparison.OrdinalIgnoreCase)) return true;
            if (t.Equals("HELADO DE 2 BOLAS", StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }

        private IEnumerable<Control> EnumerarControles(Control raiz)
        {
            foreach (Control c in raiz.Controls)
            {
                yield return c;
                if (c.HasChildren)
                {
                    foreach (Control x in EnumerarControles(c))
                        yield return x;
                }
            }
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
            /*
            var btn = sender as Button;
            var cod = btn?.Tag as string;

            if (string.IsNullOrWhiteSpace(cod))
            {
                // fallback por si no hubiera Tag
                var m = Regex.Match(btn?.Name ?? "", @"\d+");
                if (m.Success) cod = m.Value;
            }

            if (!string.IsNullOrWhiteSpace(cod))
                ProductoSeleccionado?.Invoke(cod); // ← avisa al padre*/

            var btn = sender as Control;
            if (btn == null) return;

            string cod = btn.Tag as string;
            if (string.IsNullOrWhiteSpace(cod))
            {
                var m = Regex.Match(btn.Name ?? "", @"\d+"); // btnProd##########
                if (m.Success) cod = m.Value;
            }

            if (!string.IsNullOrWhiteSpace(cod))
                ProductoSeleccionado?.Invoke(cod);  // el host decide si abre frmNHelados
        }

        private void MostrarDialogoNotasPara(Control btnProducto)
        {
            string texto = ((btnProducto != null ? btnProducto.Text : "") ?? "").Trim();
            if (texto.Length == 0) return;

            string linea = "1 x " + texto.ToUpperInvariant();

            using (var dlg = new CapaPresentacion.Notas.frmNHelados())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;

                Control[] encontrados = dlg.Controls.Find("txtProductoSelect", true);
                if (encontrados != null && encontrados.Length > 0)
                    encontrados[0].Text = linea;

                dlg.ShowDialog(this.FindForm());
            }
        }

        private void flpHelymilk_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            _dragging = true;
            _capturandoDrag = false;      // aún no sabemos si será drag
            _cancelNextClick = false;     // por defecto, permitir click

            _dragStartX = MouseXEnFlp();
            _originScrollX = -flpHelymilk.AutoScrollPosition.X;
            _lastX = _dragStartX;
            _lastDX = 0;
            _velocity = 0;
            _inertiaTimer?.Stop();

            // ❌ NO capturar aquí
            // flpBotonera.Capture = true;
            Cursor = Cursors.Hand;
        }

        private void flpHelymilk_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            int x = MouseXEnFlp();
            int delta = x - _dragStartX;

            if (!_capturandoDrag && Math.Abs(delta) > DragThreshold)
            {
                _capturandoDrag = true;
                _cancelNextClick = true;
                flpHelymilk.Capture = true;
            }

            if (!_capturandoDrag) return;

            int target = _originScrollX - delta;
            target = Math.Max(0, Math.Min(target, GetMaxScrollX()));

            int currentY = -flpHelymilk.AutoScrollPosition.Y;
            flpHelymilk.AutoScrollPosition = new Point(target, currentY);

            _lastDX = x - _lastX;
            _lastX = x;
        }

        private void flpHelymilk_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;

            _dragging = false;

            if (_capturandoDrag)
            {
                flpHelymilk.Capture = false;
                _velocity = _lastDX;
                if (Math.Abs(_velocity) > 1 && _inertiaTimer != null)
                    _inertiaTimer.Start();
            }

            Cursor = Cursors.Default;
            _capturandoDrag = false;
        }

        private void _inertiaTimer_Tick(object sender, EventArgs e)
        {
            int currentX = -flpHelymilk.AutoScrollPosition.X;
            int currentY = -flpHelymilk.AutoScrollPosition.Y;

            int target = currentX - (int)Math.Round(_velocity);
            int max = GetMaxScrollX();

            if (target < 0) { target = 0; _velocity = 0; }
            if (target > max) { target = max; _velocity = 0; }

            flpHelymilk.AutoScrollPosition = new Point(target, currentY);

            _velocity *= Decay;
            if (Math.Abs(_velocity) < 0.5 && _inertiaTimer != null) _inertiaTimer.Stop();
        }
        private int GetMaxScrollX()
        {
            int overflow = flpHelymilk.DisplayRectangle.Width - flpHelymilk.ClientSize.Width;
            return Math.Max(0, overflow);
        }

        private int MouseXEnFlp()
        {
            return flpHelymilk.PointToClient(Cursor.Position).X;
        }
    }
}
