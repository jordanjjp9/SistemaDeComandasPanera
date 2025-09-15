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
using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Helpers;
using Guna.UI2.WinForms;

namespace CapaPresentacion.Ambientes
{
    public partial class frmSPrincipal : Form
    {
        private readonly cnPedido _srv = new cnPedido();

        // mapa "001" -> botón
        //private Dictionary<string, Button> _botonesMesa;
        private Dictionary<string, Control> _botonesMesa;

        // colores (ajústalos a tu paleta)
        private readonly Color _colorLibre = Color.FromArgb(224, 224, 224);
        private readonly Color _colorOcupada = Color.RoyalBlue;
        private Timer _tRefresco;
        public frmSPrincipal()
        {
            InitializeComponent();
            this.Load += Frm_Load;
            this.StartPosition = FormStartPosition.CenterScreen;

            WireMesaButtons(this);

            // Si en el futuro agregas mesas dinámicamente:
            this.ControlAdded += (_, ev) => WireMesaButtons(ev.Control);

        }

        private void Frm_Load(object sender, EventArgs e)
        {
            // Busca y enlaza TODOS los botones que se llamen btnMesa*
            // Si tus mesas están dentro de un panel específico, cámbialo por ese panel:
            // WireMesaButtons(pnlMesas);
            WireMesaButtons(this);

            _botonesMesa = DescubrirBotonesDeMesas();

            // primer pintado
            RefrescarColoresMesas();

            // refresco periódico
            _tRefresco = new Timer { Interval = 4000 };
            _tRefresco.Tick += (s, ev) => RefrescarColoresMesas();
            _tRefresco.Start();
        }
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            try
            {
                if (_tRefresco != null)
                {
                    _tRefresco.Stop();
                    _tRefresco.Dispose();
                    _tRefresco = null;
                }
            }
            catch { }
            base.OnFormClosed(e);
        }

        /* private void frmSPrincipal_Load(object sender, EventArgs e)
         {

         }*/

        private void WireMesaButtons(Control root)
        {
            // ¿Este control es una “mesa”?
            if (root != null && root.Name.StartsWith("btnMesa", StringComparison.OrdinalIgnoreCase))
            {
                root.Click -= BtnMesa_Click;
                root.Click += BtnMesa_Click;
            }

            // Recorre hijos recursivamente
            foreach (Control c in root.Controls) WireMesaButtons(c);
        }

        private void BtnMesa_Click(object sender, EventArgs e)
        {
            var ctrl = sender as Control;
            string name = ctrl?.Name ?? "";
            string text = ctrl?.Text ?? "";

            int num = ExtraerNumero(name);
            if (num == 0) num = ExtraerNumero(text);

            // Ambiente => código "001"
            SesionActual.SetAmbiente(AmbienteTipo.Salon);

            // Mesa seleccionada
            SesionActual.Mesa = new ceMesa { Numero = num };

            // ... continuar con tu flujo (validación / abrir form etc.)
            var host = this.FindForm() as frmMesas
                       ?? this.TopLevelControl as frmMesas
                       ?? Application.OpenForms.OfType<frmMesas>().FirstOrDefault();
            host?.MostrarValidacion();
        }

        private static int ExtraerNumero(string s)
        {
            var m = Regex.Match(s ?? "", @"\d+");
            return m.Success ? int.Parse(m.Value) : 0;
        }

        // ================= PINTADO / REFRESCO =================

        /// <summary>Forzar repintado desde afuera si lo necesitas.</summary>
        //public void RefrescarColoresMesas()
        //{
        //    if (_botonesMesa == null || _botonesMesa.Count == 0) _botonesMesa = DescubrirBotonesDeMesas();

        //    foreach (var kv in _botonesMesa)
        //    {
        //        string mesa3 = kv.Key;  // "001"
        //        Button btn = kv.Value;

        //        string numPed = _srv.ObtenerNumPedAbiertoPorMesa(mesa3);
        //        bool ocupada = !string.IsNullOrEmpty(numPed);

        //        btn.BackColor = ocupada ? _colorOcupada : _colorLibre;
        //        btn.ForeColor = ocupada ? Color.White : Color.Black;
        //    }
        //}
        public void RefrescarColoresMesas()
        {
            if (_botonesMesa == null || _botonesMesa.Count == 0)
                _botonesMesa = DescubrirBotonesDeMesas();

            var srv = new CapaNegocio.cnPedido();
            foreach (var par in _botonesMesa)
            {
                string mesa3 = par.Key;
                var ctrl = par.Value;

                bool ocupada = !string.IsNullOrEmpty(srv.ObtenerNumPedAbiertoPorMesa(mesa3));
                PintarMesa(ctrl, ocupada);
            }
        }

        //private Dictionary<string, Button> DescubrirBotonesDeMesas()
        //{
        //    var dict = new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        //    foreach (var btn in EnumerarBotones(this))
        //    {
        //        string mesa3 = ObtenerCodigoMesa3(btn); // "001"
        //        if (!string.IsNullOrEmpty(mesa3) && !dict.ContainsKey(mesa3))
        //            dict.Add(mesa3, btn);
        //    }
        //    return dict;
        //}
        private Dictionary<string, Control> DescubrirBotonesDeMesas()
        {
            var dic = new Dictionary<string, Control>(StringComparer.OrdinalIgnoreCase);
            foreach (var c in EnumerarControles(this))
            {
                var mesa = ObtenerCodigoMesa3(c);
                if (!string.IsNullOrEmpty(mesa))
                    dic[mesa] = c;
            }
            return dic;
        }
        //private static IEnumerable<Button> EnumerarBotones(Control root)
        //{
        //    foreach (Control c in root.Controls)
        //    {
        //        if (c is Button b) yield return b;
        //        foreach (var child in EnumerarBotones(c)) yield return child;
        //    }
        //}
        private static IEnumerable<Control> EnumerarControles(Control root)
        {
            if (root == null) yield break;

            // ¿Es una mesa? (btnMesa*, o Tag numérico)
            bool esMesa = root.Name.StartsWith("btnMesa", StringComparison.OrdinalIgnoreCase)
                          || (root.Tag is string t && Regex.IsMatch(t, @"^\d+$"));

            // Solo controles "clickables" típicos: Guna2Button o Button
            if (esMesa && (root is Guna2Button || root is Button))
                yield return root;

            foreach (Control c in root.Controls)
                foreach (var x in EnumerarControles(c))
                    yield return x;
        }

        /// <summary>
        /// Obtiene el código de mesa a 3 dígitos desde Tag / Name / Text.
        ///  - Tag: si pones 21 → "021"
        ///  - Name/Text: "MESA 12" → "012"
        /// </summary>
        //private static string ObtenerCodigoMesa3(Button btn)
        //{
        //    // 1) Tag
        //    if (btn.Tag != null)
        //    {
        //        var s = new string(btn.Tag.ToString().Where(char.IsDigit).ToArray());
        //        if (int.TryParse(s, out int n)) return n.ToString("000");
        //    }

        //    // 2) Name o Text
        //    var m = Regex.Match(btn.Text ?? btn.Name ?? "", @"\d+");
        //    if (m.Success && int.TryParse(m.Value, out int num))
        //        return num.ToString("000");

        //    return null;
        //}
        private static string ObtenerCodigoMesa3(Control c)
        {
            string s = null;

            if (c.Tag is string tag && Regex.IsMatch(tag, @"^\d+$"))
                s = tag;
            else if (!string.IsNullOrWhiteSpace(c.Name))
            {
                var m = Regex.Match(c.Name, @"\d+");
                if (m.Success) s = m.Value;
            }
            if (string.IsNullOrWhiteSpace(s) && !string.IsNullOrWhiteSpace(c.Text))
            {
                var m2 = Regex.Match(c.Text, @"\d+");
                if (m2.Success) s = m2.Value;
            }

            if (string.IsNullOrWhiteSpace(s)) return null;
            return s.PadLeft(3, '0');
        }
        private void PintarMesa(Control c, bool ocupada)
        {
            var fondo = ocupada ? _colorOcupada : _colorLibre;
            var letra = ocupada ? Color.White : Color.Black;

            if (c is Guna2Button g)
            {
                g.FillColor = fondo;
                g.ForeColor = letra;
            }
            else if (c is Button b)
            {
                b.UseVisualStyleBackColor = false;
                b.BackColor = fondo;
                b.ForeColor = letra;
            }
            else
            {
                // fallback
                c.BackColor = fondo;
                c.ForeColor = letra;
            }
        }
    }
}
