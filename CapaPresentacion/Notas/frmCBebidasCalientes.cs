using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CapaNegocio;

namespace CapaPresentacion.Notas
{
    public partial class frmCBebidasCalientes : Form
    {
        // ==================== DTO selección ====================
        public class SeleccionSimple
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioExtra { get; set; }
        }

        // ==================== Servicios / config ====================
        private readonly cnProducto _svcProductos = new cnProducto();
        public string ListaPrecio { get; set; } = "001";

        /// <summary>Cuántas bebidas base debo elegir en este diálogo.</summary>
        public int CantidadRequerida { get; set; } = 1;

        public string Titulo { get; set; } = "Bebidas Calientes";
        public string ProductoBaseTexto { get; set; } = string.Empty;

        public List<SeleccionSimple> Selecciones { get; private set; } = new List<SeleccionSimple>();

        // Contadores
        private int _seleccionados = 0;   // bebidas base elegidas (todas, no incluye extras)
        private int _baseCafeCount = 0;   // SOLO americano + descafeinado
        private int _lecheCount = 0;      // nº de "adicional leche" elegidos

        // ==================== Reglas especiales ====================
        private const string COD_AMERICANO = "0000000330";
        private const string COD_DESCAFEINADO = "0000000225";
        private const string COD_LECHE_ENTERA = "0000000229";
        private const string COD_LECHE_SINLACTOSA = "0000000230";

        // Productos con costo forzado a 0
        private static readonly HashSet<string> CODES_PRECIO_CERO = new HashSet<string>(StringComparer.Ordinal)
        {
            "0000000623",
            "0000000625",
            "0000000624",
            "0000000333",
            "0000000330" // americano a 0 en este flujo
        };

        /// <summary>Activa la regla opcional de “adicional de leche limitado por café base”.</summary>
        public bool ReglaAdicionalLecheActiva { get; set; } = true;

        // Referencias útiles
        private Control _btnLecheEntera;
        private Control _btnLecheSinLactosa;

        // ==================== CTOR ====================
        public frmCBebidasCalientes()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            Load += frmCBebidasCalientes_Load;

            // Botones inferiores (existentes en el diseñador)
            if (btnContinuar != null) btnContinuar.Click += btnContinuar_Click;
            if (btnEliminar != null) btnEliminar.Click += btnEliminar_Click;

            // Cablea todos los botones de producto (evita Continuar/Eliminar/etc.)
            WireOptionButtons(this);
        }

        // ==================== Load ====================
        private void frmCBebidasCalientes_Load(object sender, EventArgs e)
        {
            if (lblTitle != null) lblTitle.Text = Titulo;
            if (!string.IsNullOrWhiteSpace(ProductoBaseTexto) && txtProductoSelect != null)
                txtProductoSelect.Text = ProductoBaseTexto;

            txtNotasBCalient?.Clear();

            // Ubica botones de leche por Name (ajusta si cambian en diseñador)
            _btnLecheEntera = FindByName("btnProd0000000229");
            _btnLecheSinLactosa = FindByName("btnProd0000000230");

            ActualizarEstado();
            UpdateBaseButtonsEnabled();
            UpdateExtrasEnabled();
        }

        // ==================== Click de una opción (producto) ====================
        private void Opcion_Click(object sender, EventArgs e)
        {
            var ctrl = sender as Control;
            if (!EsBotonOpcion(ctrl)) return;

            var opt = ParseOpcionFromControl(ctrl);
            if (opt == null) return;

            string cod10 = Canon(opt.Codigo);
            bool esExtraLeche = IsExtraLeche(cod10);

            // 1) Tope para "adicional leche" (opcional)
            if (ReglaAdicionalLecheActiva && esExtraLeche && _baseCafeCount <= _lecheCount)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            // 2) Límite de bebidas base = CantidadRequerida
            if (!esExtraLeche && _seleccionados >= CantidadRequerida)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            // 3) Registrar
            Selecciones.Add(opt);

            // 4) Contadores
            if (!esExtraLeche) _seleccionados++;
            if (IsBaseCafe(cod10)) _baseCafeCount++;
            else if (esExtraLeche) _lecheCount++;

            // 5) Visual
            AppendLineaNota(opt);
            ActualizarEstado();
            UpdateBaseButtonsEnabled();
            UpdateExtrasEnabled();
        }

        // ==================== Notas (panel derecho) ====================
        private void AppendLineaNota(SeleccionSimple opt)
        {
            if (txtNotasBCalient == null) return;

            var sb = new StringBuilder();
            sb.Append("- ").Append(opt.Descripcion);
            if (opt.PrecioExtra > 0m)
                sb.Append(" = S/ ").Append(opt.PrecioExtra.ToString("0.00", CultureInfo.InvariantCulture));

            if (txtNotasBCalient.TextLength > 0) txtNotasBCalient.AppendText(Environment.NewLine);
            txtNotasBCalient.AppendText(sb.ToString());
            txtNotasBCalient.SelectionStart = txtNotasBCalient.TextLength;
            txtNotasBCalient.ScrollToCaret();
        }

        private void RebuildNotas()
        {
            if (txtNotasBCalient == null) return;

            var sb = new StringBuilder();
            for (int i = 0; i < Selecciones.Count; i++)
            {
                if (i > 0) sb.AppendLine();
                var s = Selecciones[i];
                sb.Append("- ").Append(s.Descripcion);
                if (s.PrecioExtra > 0m)
                    sb.Append(" = S/ ").Append(s.PrecioExtra.ToString("0.00", CultureInfo.InvariantCulture));
            }

            txtNotasBCalient.Text = sb.ToString();
            txtNotasBCalient.SelectionStart = txtNotasBCalient.TextLength;
            txtNotasBCalient.ScrollToCaret();
        }

        // ==================== Parseo botón -> SeleccionSimple ====================
        private SeleccionSimple ParseOpcionFromControl(Control c)
        {
            // 1) Tag como DTO
            if (c.Tag is SeleccionSimple dto)
            {
                var r = new SeleccionSimple
                {
                    Codigo = dto.Codigo,
                    Descripcion = dto.Descripcion,
                    PrecioExtra = ForzarPrecioSiAplica(dto.Codigo, dto.PrecioExtra)
                };
                if (r.PrecioExtra < 0m) r.PrecioExtra = ObtenerPrecioDeCodigo(r.Codigo);
                return r;
            }

            // 2) Tag "COD|Desc|Precio?"
            if (c.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
            {
                var parts = tag.Split('|');
                if (parts.Length >= 2)
                {
                    var cod = parts[0].Trim();
                    var des = parts[1].Trim();
                    decimal precio = -1m;
                    if (parts.Length >= 3)
                        decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out precio);

                    precio = ForzarPrecioSiAplica(cod, precio);
                    if (precio < 0m) precio = ObtenerPrecioDeCodigo(cod);

                    return new SeleccionSimple { Codigo = cod, Descripcion = des, PrecioExtra = precio };
                }
            }

            // 3) Fallback: código por Name
            string codigo = "";
            var m = Regex.Match(c.Name ?? "", @"\d+");
            if (m.Success) codigo = m.Value;

            var precioFinal = ForzarPrecioSiAplica(codigo, -1m);
            if (precioFinal < 0m) precioFinal = ObtenerPrecioDeCodigo(codigo);

            return new SeleccionSimple
            {
                Codigo = codigo,
                Descripcion = (c.Text ?? "").Trim(),
                PrecioExtra = precioFinal
            };
        }

        private decimal ForzarPrecioSiAplica(string codigo, decimal precioActual)
        {
            string cod10 = Canon(codigo);
            if (CODES_PRECIO_CERO.Contains(cod10)) return 0m;
            return precioActual; // <0 => consultar lista
        }

        private decimal ObtenerPrecioDeCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return 0m;
            if (CODES_PRECIO_CERO.Contains(Canon(codigo))) return 0m;

            try
            {
                var p = _svcProductos.Obtener(Canon(codigo), ListaPrecio);
                if (p == null) return 0m;
                return (p.PrecioUnitario != 0m) ? p.PrecioUnitario : p.ValorUnitario;
            }
            catch { return 0m; }
        }

        // ==================== Estado / botones inferiores ====================
        private void ActualizarEstado()
        {
            int faltan = Math.Max(0, CantidadRequerida - _seleccionados);
            if (lblEstado != null)
                lblEstado.Text = $"Seleccionados: {_seleccionados}/{CantidadRequerida} • Faltan: {faltan}";

            if (btnContinuar != null)
                btnContinuar.Enabled = (_seleccionados == CantidadRequerida);

            if (btnEliminar != null)
                btnEliminar.Enabled = (Selecciones.Count > 0);
        }

        private void btnContinuar_Click(object sender, EventArgs e)
        {
            if (_seleccionados != CantidadRequerida) return;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (Selecciones.Count == 0) return;

            var removed = Selecciones[Selecciones.Count - 1];
            Selecciones.RemoveAt(Selecciones.Count - 1);

            string cod10 = Canon(removed.Codigo);
            bool esExtraLeche = IsExtraLeche(cod10);

            if (!esExtraLeche && _seleccionados > 0) _seleccionados--;
            if (IsBaseCafe(cod10) && _baseCafeCount > 0) _baseCafeCount--;
            else if (esExtraLeche && _lecheCount > 0) _lecheCount--;

            RebuildNotas();
            ActualizarEstado();
            UpdateBaseButtonsEnabled();
            UpdateExtrasEnabled();
        }

        // ==================== Habilitación de botones ====================
        private void UpdateBaseButtonsEnabled()
        {
            bool puedeElegirBase = (_seleccionados < CantidadRequerida);

            foreach (var b in EnumerarBotonesProducto())
            {
                string cod = CodigoDeControl(b);
                if (IsExtraLeche(cod)) continue; // extras se controlan aparte
                SetEnabled(b, puedeElegirBase);
            }
        }

        private void UpdateExtrasEnabled()
        {
            if (!ReglaAdicionalLecheActiva)
            {
                SetEnabled(_btnLecheEntera, true);
                SetEnabled(_btnLecheSinLactosa, true);
                return;
            }

            bool canPickExtra = (_baseCafeCount > 0) && (_lecheCount < _baseCafeCount);
            SetEnabled(_btnLecheEntera, canPickExtra);
            SetEnabled(_btnLecheSinLactosa, canPickExtra);
        }

        private void SetEnabled(Control c, bool enabled)
        {
            if (c == null) return;
            c.Enabled = enabled;
            c.Cursor = enabled ? Cursors.Hand : Cursors.No;
        }

        // ==================== Utilidades de árbol de controles ====================
        private Control FindByName(string name) =>
            string.IsNullOrWhiteSpace(name) ? null : this.Controls.Find(name, true).FirstOrDefault();

        private IEnumerable<Control> EnumerarBotonesProducto()
        {
            var stack = new Stack<Control.ControlCollection>();
            stack.Push(this.Controls);
            while (stack.Count > 0)
            {
                foreach (Control c in stack.Pop())
                {
                    if (c.HasChildren) stack.Push(c.Controls);
                    if (EsBotonOpcion(c)) yield return c;
                }
            }
        }

        private static string Canon(string cod) => (cod ?? string.Empty).Trim().PadLeft(10, '0');

        private static bool IsBaseCafe(string cod10) =>
            Canon(cod10) == COD_AMERICANO || Canon(cod10) == COD_DESCAFEINADO;

        private static bool IsExtraLeche(string cod10) =>
            Canon(cod10) == COD_LECHE_ENTERA || Canon(cod10) == COD_LECHE_SINLACTOSA;

        private static string CodigoDeControl(Control c)
        {
            if (c == null) return string.Empty;

            if (c.Tag is SeleccionSimple dto && !string.IsNullOrWhiteSpace(dto.Codigo))
                return Canon(dto.Codigo);

            if (c.Tag is string tag && !string.IsNullOrWhiteSpace(tag))
            {
                var parts = tag.Split('|');
                if (parts.Length >= 1 && !string.IsNullOrWhiteSpace(parts[0]))
                    return Canon(parts[0].Trim());
            }

            var m = Regex.Match(c.Name ?? "", @"\d+");
            if (m.Success) return Canon(m.Value);

            return string.Empty;
        }

        /// <summary>
        /// Devuelve true solo para botones de producto (no para Continuar/Eliminar/etc.).
        /// </summary>
        private bool EsBotonOpcion(Control c)
        {
            if (c == null) return false;

            // Excluir botones de acción por referencia o nombre
            if (ReferenceEquals(c, btnContinuar) ||
                ReferenceEquals(c, btnEliminar) ||
                string.Equals(c.Name, "btnCerrar", StringComparison.OrdinalIgnoreCase))
                return false;

            // Aceptar por convención de nombre
            if ((c.Name ?? "").StartsWith("btnProd", StringComparison.OrdinalIgnoreCase))
                return true;

            // O si el Tag trae info de producto
            if (c.Tag is SeleccionSimple) return true;
            if (c.Tag is string s && s.Split('|').Length >= 2) return true;

            return false;
        }
        // Recorre todo el árbol de controles y conecta solo los botones de producto
        private void WireOptionButtons(Control root)
        {
            if (root == null) return;

            bool esBoton = root is Button ||
                           ((root.GetType().FullName ?? "")
                            .IndexOf("Guna2Button", StringComparison.OrdinalIgnoreCase) >= 0);

            if (esBoton)
            {
                // Por si ya estaba enganchado
                root.Click -= Opcion_Click;

                // Solo conectamos si es un botón de producto (no Continuar/Eliminar/etc.)
                if (EsBotonOpcion(root))
                    root.Click += Opcion_Click;
            }

            foreach (Control c in root.Controls)
                WireOptionButtons(c);
        }

    }
}
