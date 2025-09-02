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
    public partial class frmBebidasF : Form, ISelectorProducto
    {
        public event Action<string> ProductoSeleccionado;

        private bool _cancelNextClick = false;
        //private bool _dragging = false;
        private int _dragStartX = 0;
        private int _originScrollX = 0;
        private int _lastX = 0;
        private int _lastDX = 0;

        private bool _capturandoDrag = false;
        private const int DragThreshold = 6;
        // private bool _cancelNextClick = false;

        // Inercia
    //    private System.Windows.Forms.Timer _inertiaTimer;
        private double _velocity = 0;       // px por tick (~15 ms)
        private const double Decay = 0.90;  // 0.85–0.95
        public frmBebidasF()
        {
            InitializeComponent();
            this.Load += frmBebidasF_Load;
        }

        private void frmBebidasF_Load(object sender, EventArgs e)
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
    }
}
