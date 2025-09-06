using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CapaNegocio; // si prefieres resolver precio por código

namespace CapaPresentacion.Notas
{
    public partial class frmCDesayunoTamal : Form
    {
        // ============ DTO ============
        public class SeleccionSimple
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioExtra { get; set; }
        }

        // ============ ENTRADAS ============
        /// <summary>Cuántos tamales en total debe elegir el usuario (por ejemplo, 1 por Criollo, 2 por Panera, multiplicado por la cantidad de desayunos si aplica).</summary>
        public int CantidadRequerida { get; set; } = 1;

        /// <summary>Texto que se muestra arriba como referencia (ej: "1 x DESAYUNO CRIOLLO").</summary>
        public string ProductoBaseTexto { get; set; } = string.Empty;

        /// <summary>Lista de precio a consultar cuando se pasa solo el código en Tag.</summary>
        public string ListaPrecio { get; set; } = "001";

        /// <summary>Título del formulario (por defecto “Elige tamales”).</summary>
        public string Titulo { get; set; } = "Elige tamales";

        // ============ SALIDA ============
        public List<SeleccionSimple> Selecciones { get; private set; } = new List<SeleccionSimple>();

        // ============ INTERNOS ============
        private int _seleccionados = 0;
        private readonly cnProducto _svcProductos = new cnProducto();

        // Controles comunes (si existen en tu diseñador)
        private Control _txtProducto;     // txtProductoSelect (readonly)
        private TextBoxBase _txtResumen;  // multiline para ver lo elegido (ej: txtNotasTamal / txtResumen)
        private Button _btnContinuar;
        private Button _btnEliminar;

        // Si tienes un FlowLayoutPanel donde están SOLO las opciones (chips), nómbralo flpOpciones
        private FlowLayoutPanel _flpOpciones;

        public frmCDesayunoTamal()
        {
            InitializeComponent();

            StartPosition = FormStartPosition.CenterParent;
            Load += Frm_Load;
        }

        // ==================== LOAD ====================
        private void Frm_Load(object sender, EventArgs e)
        {
            // 1) Título
            var lblTitle = this.Controls.Find("lblTitle", true).FirstOrDefault();
            if (lblTitle != null) lblTitle.Text = Titulo;

            // 2) Encabezado con el producto base
            _txtProducto = this.Controls.Find("txtProductoSelect", true).FirstOrDefault();
            if (_txtProducto != null && !string.IsNullOrWhiteSpace(ProductoBaseTexto))
                TrySetText(_txtProducto, ProductoBaseTexto);

            // 3) Resolver controles de acción
            _btnContinuar = this.Controls.Find("btnContinuar", true).OfType<Button>().FirstOrDefault();
            _btnEliminar = this.Controls.Find("btnEliminar", true).OfType<Button>().FirstOrDefault();

            if (_btnContinuar != null)
            {
                _btnContinuar.Click -= btnContinuar_Click;
                _btnContinuar.Click += btnContinuar_Click;
                this.AcceptButton = _btnContinuar;
            }
            if (_btnEliminar != null)
            {
                _btnEliminar.Click -= btnEliminar_Click;
                _btnEliminar.Click += btnEliminar_Click;
            }

            // 4) Buscar un TextBox multiline para el resumen (izq/dcha)
            _txtResumen = FindResumenTextBox(); // intenta encontrar txtNotasTamal / txtResumen...
            RedibujarResumen();

            // 5) Panel de “chips” (opciones)
            _flpOpciones = this.Controls.Find("flpOpciones", true).OfType<FlowLayoutPanel>().FirstOrDefault();
            if (_flpOpciones != null)
                WireOptionButtons(_flpOpciones);              // engancha SOLO los hijos de flpOpciones
            else
                WireOptionButtons(this);                      // fallback: engancha en todo el form (filtra Continuar/Eliminar)

            ActualizarEstado();
        }

        // ==================== WIRING DE OPCIONES ====================
        private void WireOptionButtons(Control root)
        {
            if (root == null) return;

            foreach (Control c in root.Controls)
            {
                // recorre recursivo
                WireOptionButtons(c);

                // detecta botones (Button, Guna2Button u otros que terminen en Button)
                bool esBoton =
                    (c is Button) ||
                    (c.GetType().Name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    ((c.Name ?? "").StartsWith("btn", StringComparison.OrdinalIgnoreCase));

                if (!esBoton) continue;

                // Excluir acciones
                if (ReferenceEquals(c, _btnContinuar) || ReferenceEquals(c, _btnEliminar)) continue;
                string n = (c.Name ?? "").Trim().ToUpperInvariant();
                string t = (c.Text ?? "").Trim().ToUpperInvariant();
                if (n == "BTNCONTINUAR" || n == "BTNELIMINAR") continue;
                if (t == "CONTINUAR" || t == "ELIMINAR") continue;

                // Engancha click de opción
                c.Click -= Opcion_Click;
                c.Click += Opcion_Click;
            }
        }

        // ==================== CLICK EN OPCIÓN ====================
        private void Opcion_Click(object sender, EventArgs e)
        {
            if (_seleccionados >= CantidadRequerida) { System.Media.SystemSounds.Beep.Play(); return; }

            var ctrl = sender as Control;
            if (ctrl == null) return;

            var opt = ParseOpcionFromControl(ctrl);
            if (opt == null) return;

            Selecciones.Add(opt);
            _seleccionados++;

            RedibujarResumen();
            ActualizarEstado();
        }

        // ==================== ELIMINAR ÚLTIMA ====================
        private void btnEliminar_Click(object sender, EventArgs e)
        {
            if (_seleccionados <= 0) return;

            int last = Selecciones.Count - 1;
            if (last >= 0)
            {
                Selecciones.RemoveAt(last);
                _seleccionados--;
            }

            RedibujarResumen();
            ActualizarEstado();
        }

        // ==================== CONTINUAR ====================
        private void btnContinuar_Click(object sender, EventArgs e)
        {
            if (_seleccionados != CantidadRequerida)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            this.DialogResult = DialogResult.OK;
            Close();
        }

        // ==================== RESUMEN / ESTADO ====================
        private void RedibujarResumen()
        {
            if (_txtResumen == null) return;

            // Si quieres listar “uno por línea”:
            var sb = new StringBuilder();
            for (int i = 0; i < Selecciones.Count; i++)
            {
                var s = Selecciones[i];
                if (i > 0) sb.AppendLine();
                sb.Append("- ").Append(s.Descripcion);
                if (s.PrecioExtra > 0m)
                    sb.Append(" = S/ ").Append(s.PrecioExtra.ToString("0.00", CultureInfo.InvariantCulture));
            }
            _txtResumen.Text = sb.ToString();
            _txtResumen.SelectionStart = _txtResumen.TextLength;
            _txtResumen.ScrollToCaret();
        }

        private void ActualizarEstado()
        {
            var lblEstado = this.Controls.Find("lblEstado", true).FirstOrDefault();
            if (lblEstado != null)
                lblEstado.Text = string.Format("Seleccionados: {0}/{1}", _seleccionados, CantidadRequerida);

            if (_btnContinuar != null)
                _btnContinuar.Enabled = (_seleccionados == CantidadRequerida);

            if (_btnEliminar != null)
                _btnEliminar.Enabled = (_seleccionados > 0);
        }

        // ==================== PARSEO DE BOTÓN ====================
        private SeleccionSimple ParseOpcionFromControl(Control c)
        {
            // 1) Tag como DTO
            var dto = c.Tag as SeleccionSimple;
            if (dto != null)
            {
                var r1 = new SeleccionSimple
                {
                    Codigo = dto.Codigo,
                    Descripcion = dto.Descripcion,
                    PrecioExtra = dto.PrecioExtra
                };
                // Si no trae precio, intenta resolver por código
                if (r1.PrecioExtra == 0m && !string.IsNullOrWhiteSpace(r1.Codigo))
                    r1.PrecioExtra = ObtenerPrecioDeCodigo(r1.Codigo);
                return r1;
            }

            // 2) Tag como "COD|Desc|Precio?"
            var tag = c.Tag as string;
            if (!string.IsNullOrWhiteSpace(tag))
            {
                var parts = tag.Split('|');
                if (parts.Length >= 2)
                {
                    var cod = parts[0].Trim();
                    var des = parts[1].Trim();
                    decimal precio = 0m;
                    if (parts.Length >= 3)
                        decimal.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out precio);
                    if (precio == 0m && !string.IsNullOrWhiteSpace(cod))
                        precio = ObtenerPrecioDeCodigo(cod);

                    return new SeleccionSimple
                    {
                        Codigo = cod,
                        Descripcion = des,
                        PrecioExtra = precio
                    };
                }
            }

            // 3) Fallback: deduce código desde Name si tiene dígitos
            string codigo = "";
            var m = Regex.Match(c.Name ?? "", @"\d+");
            if (m.Success) codigo = m.Value;

            return new SeleccionSimple
            {
                Codigo = codigo,
                Descripcion = (c.Text ?? "").Trim(),
                PrecioExtra = string.IsNullOrWhiteSpace(codigo) ? 0m : ObtenerPrecioDeCodigo(codigo)
            };
        }

        private decimal ObtenerPrecioDeCodigo(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return 0m;
            var cod10 = codigo.Trim().PadLeft(10, '0');
            try
            {
                var p = _svcProductos.Obtener(cod10, ListaPrecio);
                if (p == null) return 0m;
                return p.PrecioUnitario != 0m ? p.PrecioUnitario : p.ValorUnitario;
            }
            catch { return 0m; }
        }

        // ==================== HELPERS DE UI ====================
        private TextBoxBase FindResumenTextBox()
        {
            // Busca por nombres típicos
            var candidatos = new[] { "txtNotasTamal", "txtResumen", "txtSeleccion", "txtNotas", "txtDetalle" };
            foreach (var name in candidatos)
            {
                var tb = this.Controls.Find(name, true).OfType<TextBoxBase>().FirstOrDefault();
                if (tb != null) return tb;
            }
            // fallback: primer TextBoxBase multilínea que no sea txtProductoSelect
            var firstMulti = this.Controls
                .OfType<TextBoxBase>()
                .FirstOrDefault(t => t.Multiline && !string.Equals(t.Name, "txtProductoSelect", StringComparison.OrdinalIgnoreCase));
            return firstMulti;
        }

        private static void TrySetText(Control c, string text)
        {
            if (c == null) return;
            var prop = c.GetType().GetProperty("Text");
            if (prop != null && prop.CanWrite) prop.SetValue(c, text ?? string.Empty, null);
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
