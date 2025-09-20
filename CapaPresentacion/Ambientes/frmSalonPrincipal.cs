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

        // Inicio (UTC) de cada mesa con pedido abierto
        private readonly Dictionary<int, DateTime> _inicioMesaUtc = new Dictionary<int, DateTime>();

        // Timer general que actualiza visualmente HH:mm:ss cada segundo
        private readonly Timer _timerMesas = new Timer { Interval = 1000 }; // 1 s

        // Mapa "001" -> control (botón) de mesa
        private Dictionary<string, Control> _botonesMesa;

        // Colores para libre/ocupada
        private readonly Color _colorLibre = Color.FromArgb(224, 224, 224);
        private readonly Color _colorOcupada = Color.RoyalBlue;

        // Timer que refresca estado (BD) cada N segundos
        private Timer _tRefresco;

        public frmSPrincipal()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;

            this.Load += Frm_Load;
            this.ControlAdded += (_, ev) => WireMesaButtons(ev.Control);

            _timerMesas.Tick += TimerMesas_Tick;
            _timerMesas.Start();

            WireMesaButtons(this);
        }

        // ===== Ciclos de vida =====
        private void Frm_Load(object sender, EventArgs e)
        {
            // Cablea por si acaso (si hay contenedores)
            WireMesaButtons(this);

            // Descubre todos los botones de mesas y primer refresco
            _botonesMesa = DescubrirBotonesDeMesas();
            RefrescarMesasYTimers();

            // Refresco periódico desde BD (colores + timers)
            _tRefresco = new Timer { Interval = 4000 };
            _tRefresco.Tick += (s, ev) => RefrescarMesasYTimers();
            _tRefresco.Start();
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            // Al volver a esta ventana, sincroniza inmediatamente
            RefrescarMesasYTimers();
        }
        public void RefrescarEstadoMesas()
        {
            RefrescarMesasYTimers(); // método privado interno
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
                _timerMesas.Stop();
                _timerMesas.Dispose();
            }
            catch { /* ignorar */ }

            base.OnFormClosed(e);
        }

        // ===== Cableado de mesas (clic) =====
        private void WireMesaButtons(Control root)
        {
            if (root == null) return;

            if (root.Name.StartsWith("btnMesa", StringComparison.OrdinalIgnoreCase))
            {
                root.Click -= BtnMesa_Click;
                root.Click += BtnMesa_Click;
            }

            foreach (Control c in root.Controls)
                WireMesaButtons(c);
        }

        private void BtnMesa_Click(object sender, EventArgs e)
        {
            var ctrl = sender as Control;
            string name = ctrl?.Name ?? "";
            string text = ctrl?.Text ?? "";

            int num = ExtraerNumero(name);
            if (num == 0) num = ExtraerNumero(text);

            // Ambiente => SALÓN
            SesionActual.SetAmbiente(AmbienteTipo.Salon);

            // Mesa seleccionada
            SesionActual.Mesa = new ceMesa { Numero = num };

            // Mostrar validación / abrir flujo
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

        // ===== Refresco unificado (colores + timers) =====
        private void RefrescarMesasYTimers()
        {
            if (_botonesMesa == null || _botonesMesa.Count == 0)
                _botonesMesa = DescubrirBotonesDeMesas();

            foreach (var par in _botonesMesa)
            {
                string mesa3 = par.Key;            // "001"
                int mesaNum = int.Parse(mesa3);    // 1..N
                var ctrl = par.Value;

                // Si este método ya filtra SWT_PED<>'T', devuelve vacío cuando está cerrada:
                string numPed8 = _srv.ObtenerNumPedAbiertoPorMesa(mesa3);

                if (string.IsNullOrWhiteSpace(numPed8))
                {
                    // No hay pedido abierto
                    PintarMesa(ctrl, false);
                    StopMesa(mesaNum);
                    continue;
                }

                // Hay pedido abierto => pinta ocupada y sincroniza timer
                PintarMesa(ctrl, true);

                var cab = _srv.ObtenerCabeceraPorNum(numPed8);
                bool cerrada = (cab != null) && string.Equals(cab.SWT_PED, "T", StringComparison.OrdinalIgnoreCase);
                if (cerrada)
                {
                    // Por seguridad, si la cabecera indica cerrada
                    PintarMesa(ctrl, false);
                    StopMesa(mesaNum);
                    continue;
                }

                var fec = (cab != null && cab.FEC_PED != default(DateTime)) ? cab.FEC_PED : DateTime.Now;
                StartMesa(mesaNum, fec);
            }
        }

        // ===== Buscar controles de mesas =====
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

        private static IEnumerable<Control> EnumerarControles(Control root)
        {
            if (root == null) yield break;

            bool esMesa = root.Name.StartsWith("btnMesa", StringComparison.OrdinalIgnoreCase)
                          || (root.Tag is string t && Regex.IsMatch(t, @"^\d+$"));

            if (esMesa && (root is Guna2Button || root is Button))
                yield return root;

            foreach (Control c in root.Controls)
                foreach (var x in EnumerarControles(c))
                    yield return x;
        }

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

        // ===== Pintado de botones =====
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
                c.BackColor = fondo;
                c.ForeColor = letra;
            }
        }

        // ===== Timers por mesa =====
        private void TimerMesas_Tick(object sender, EventArgs e)
        {
            ActualizarTodosLosTimers();
        }

        /// <summary> Arranca/actualiza el contador de la mesa con la hora real de creación del pedido (local). </summary>
        public void StartMesa(int mesa, DateTime fecPedLocal)
        {
            _inicioMesaUtc[mesa] = fecPedLocal.ToUniversalTime();

            var lbl = GetLblMesa(mesa);   // Control
            if (lbl != null)
            {
                lbl.Visible = true;
                lbl.Text = "00:00:00";    // todos estos tipos tienen .Text
            }
        }

        /// <summary> Detiene/oculta el contador de la mesa (pedido cerrado). </summary>
        public void StopMesa(int mesa)
        {
            if (_inicioMesaUtc.ContainsKey(mesa))
                _inicioMesaUtc.Remove(mesa);

            var lbl = GetLblMesa(mesa);   // Control
            if (lbl != null)
                lbl.Visible = false;
        }

        /// <summary> Actualiza todos los labels a hh:mm:ss. </summary>
        private void ActualizarTodosLosTimers()
        {
            if (_inicioMesaUtc.Count == 0) return;

            var ahoraUtc = DateTime.UtcNow;

            foreach (var kv in _inicioMesaUtc.ToArray())
            {
                int mesa = kv.Key;
                DateTime inicioUtc = kv.Value;

                var lbl = GetLblMesa(mesa);   // Control
                if (lbl == null) continue;

                var span = ahoraUtc - inicioUtc;
                if (span < TimeSpan.Zero) span = TimeSpan.Zero;

                var horas = span.Hours + span.Days * 24;
                var ts = new TimeSpan(horas, span.Minutes, span.Seconds);

                lbl.Text = ts.ToString(@"hh\:mm\:ss");
                lbl.Visible = true;
            }
        }

        // Busca lblTimerMesaN (Label clásico)
        private Control GetLblMesa(int mesa)
        {
            var name = "lblTimerMesa" + mesa;
            var arr = this.Controls.Find(name, true); // búsqueda recursiva
            return (arr != null && arr.Length > 0) ? arr[0] : null; // <-- devuelve Control
        }
    }
}
