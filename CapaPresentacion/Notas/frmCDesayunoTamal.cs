using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CapaNegocio;

namespace CapaPresentacion.Notas
{
    public partial class frmCDesayunoTamal : Form
    {
        // ===== DTO =====
        public class SeleccionSimple
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioExtra { get; set; }
        }

        // ===== Entradas =====
        public int CantidadRequerida { get; set; } = 1;
        public string ProductoBaseTexto { get; set; } = string.Empty;
        public string ListaPrecio { get; set; } = "001";
        public string Titulo { get; set; } = "Elige tamales";

        // ===== Salida =====
        public List<SeleccionSimple> Selecciones { get; private set; } = new List<SeleccionSimple>();

        // ===== Internos =====
        private int _seleccionados = 0;
        private readonly cnProducto _svcProductos = new cnProducto();

        private Control _txtProducto;      // txtProductoSelect
        private Control _txtNotasTamal;    // txtNotasTamal (puede ser Guna2TextBox o TextBox)
        //private Button _btnContinuar;
        //private Button _btnEliminar;
        private Control _btnContinuar;
        private Control _btnEliminar;
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
            Text = string.IsNullOrWhiteSpace(Titulo) ? "Seleccionar Tamal" : Titulo;

            // 1) Resolver controles
            _txtProducto = Controls.Find("txtProductoSelect", true).FirstOrDefault();
            if (_txtProducto != null && !string.IsNullOrWhiteSpace(ProductoBaseTexto))
                TrySetText(_txtProducto, ProductoBaseTexto);

            // IMPORTANTE: buscar como Control (Guna2TextBox no es TextBoxBase)
            _txtNotasTamal = Controls.Find("txtNotasTamal", true).FirstOrDefault();

            _btnContinuar = Controls.Find("btnContinuar", true).FirstOrDefault();
            _btnEliminar = Controls.Find("btnEliminar", true).FirstOrDefault();

            if (_btnContinuar != null)
            {
                _btnContinuar.Click -= btnContinuar_Click;
                _btnContinuar.Click += btnContinuar_Click;

                if (_btnContinuar is IButtonControl ib)   // Guna2Button suele implementarlo
                    this.AcceptButton = ib;
            }

            // MUY IMPORTANTE: que no queden cableados como opción
            if (_btnContinuar != null) _btnContinuar.Click -= Opcion_Click;
            if (_btnEliminar != null)
            {
                _btnEliminar.Click -= btnEliminar_Click;
                _btnEliminar.Click += btnEliminar_Click;
                _btnEliminar.Click -= Opcion_Click;
            }

            // 2) Cablear botones de opciones
            _flpOpciones = Controls.Find("flpOpciones", true).OfType<FlowLayoutPanel>().FirstOrDefault();
            if (_flpOpciones != null) WireOptionButtonsRecursive(_flpOpciones);
            else WireOptionButtonsRecursive(this); // fallback: todo el form

            // 3) Estado inicial
            RedibujarNotasTamal();
            ActualizarEstado();
        }

        // ==================== WIRING OPCIONES ====================
        private void WireOptionButtonsRecursive(Control root)
        {
            if (root == null) return;

            foreach (Control c in root.Controls)
                WireOptionButtonsRecursive(c);

            if (EsAccion(root)) return;              // <- excluye Continuar/Eliminar

            if (EsBotonOpcion(root))
            {
                root.Click -= Opcion_Click;
                root.Click += Opcion_Click;
            }
        }
        private bool EsAccion(Control c)
        {
            if (c == null) return false;

            // por referencia
            if (ReferenceEquals(c, _btnContinuar) || ReferenceEquals(c, _btnEliminar))
                return true;

            // por nombre (por si no los resolvió)
            var n = (c.Name ?? "").ToLowerInvariant();
            if (n == "btncontinuar" || n == "btneliminar")
                return true;

            // opcional: si marcas Tag="accion" en los botones de pie
            if (string.Equals(c.Tag as string, "accion", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        private static bool EsBotonOpcion(Control c)
        {
            if (c == null) return false;

            // Lo típico: Button o controles cuyo tipo contiene "Button"
            if (c is Button) return true;
            var typeName = c.GetType().Name ?? "";
            if (typeName.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0) return true;

            // También aceptamos controles "tile/panel" que tengan una propiedad Text visible
            // (Guna2TileButton, etc.). Si no quieres que algo cuente, marca su Tag = "no-opcion".
            if (string.Equals(c.Tag as string, "no-opcion", StringComparison.OrdinalIgnoreCase)) return false;

            return c.GetType().GetProperty("Text") != null;
        }

        // ==================== CLICK EN OPCIÓN ====================
        private void Opcion_Click(object sender, EventArgs e)
        {
            if (_seleccionados >= CantidadRequerida)
            {
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            var ctrl = sender as Control;
            if (ctrl == null) return;

            var sel = ParseOpcionFromControl(ctrl);
            if (sel == null) return;

            Selecciones.Add(sel);
            _seleccionados++;

            RedibujarNotasTamal();
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

            RedibujarNotasTamal();
            ActualizarEstado();
        }

        // ==================== CONTINUAR ====================
        private void btnContinuar_Click(object sender, EventArgs e)
        {
            if (_seleccionados < CantidadRequerida)
            {
                MessageBox.Show($"Debes seleccionar {CantidadRequerida} opción(es).",
                                "Tamal", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        // ==================== RESUMEN ====================
        private void RedibujarNotasTamal()
        {
            if (_txtNotasTamal == null) return;

            string s = BuildNotasTamal();
            TrySetText(_txtNotasTamal, s);
        }

        private string BuildNotasTamal()
        {
            if (Selecciones == null || Selecciones.Count == 0) return string.Empty;

            var grupos = Selecciones
                .GroupBy(s => new { D = (s.Descripcion ?? "").Trim().ToUpperInvariant(), P = s.PrecioExtra })
                .Select(g => new
                {
                    Cant = g.Count(),
                    Desc = g.Key.D,
                    Precio = g.Key.P,
                    Total = g.Count() * g.Key.P
                })
                .ToList();

            var sb = new StringBuilder();
            foreach (var x in grupos)
            {
                if (sb.Length > 0) sb.AppendLine();

                if (x.Cant <= 1)
                {
                    if (x.Precio > 0m)
                        sb.Append("- ").Append(x.Desc).Append(" = S/ ").Append(x.Total.ToString("0.00", CultureInfo.InvariantCulture));
                    else
                        sb.Append("- ").Append(x.Desc);
                }
                else
                {
                    if (x.Precio > 0m)
                        sb.Append("- ").Append(x.Cant).Append(" x ").Append(x.Desc).Append(" = S/ ").Append(x.Total.ToString("0.00", CultureInfo.InvariantCulture));
                    else
                        sb.Append("- ").Append(x.Cant).Append(" x ").Append(x.Desc);
                }
            }
            return sb.ToString();
        }

        private void ActualizarEstado()
        {
            if (_btnContinuar != null) _btnContinuar.Enabled = (_seleccionados >= CantidadRequerida);
            if (_btnEliminar != null) _btnEliminar.Enabled = (_seleccionados > 0);

            Text = $"{Titulo}  ({_seleccionados}/{CantidadRequerida})";
        }

        // ==================== PARSEO ====================
        private SeleccionSimple ParseOpcionFromControl(Control c)
        {
            string desc = (c.Text ?? string.Empty).Trim();
            if (desc.Length == 0) return null;

            // Si usas Tag para código/precio, levántalos aquí; por defecto usamos Name y 0 extra:
            string codigo = c.Name ?? string.Empty;
            decimal precio = 0m;
            // if (c.Tag is decimal d) precio = d;

            return new SeleccionSimple
            {
                Codigo = codigo,
                Descripcion = desc,
                PrecioExtra = precio
            };
        }

        // ==================== HELPERS ====================
        private static void TrySetText(Control ctrl, string text)
        {
            if (ctrl == null) return;
            try
            {
                var p = ctrl.GetType().GetProperty("Text");
                p?.SetValue(ctrl, text ?? string.Empty, null);
            }
            catch { /* ignore */ }
        }

        private void btnCerrar_Click(object sender, EventArgs e) => Close();
    }
}
