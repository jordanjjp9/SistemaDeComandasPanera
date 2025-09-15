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
            public string Codigo { get; set; }       // CDG_PROD (10 dígitos)
            public string Descripcion { get; set; }
            public decimal PrecioExtra { get; set; }
            public string Notas { get; set; }
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
        private Control _txtNotasTamal;    // txtNotasTamal (Guna2TextBox o TextBox)
        private Control _btnContinuar;
        private Control _btnEliminar;
        private FlowLayoutPanel _flpOpciones;

        // === AJUSTA AQUÍ si tus códigos reales son distintos ===
        private const string COD_TAMAL_CERDO = "0000001106";
        private const string COD_TAMAL_POLLO = "0000001107";

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

            _txtNotasTamal = Controls.Find("txtNotasTamal", true).FirstOrDefault();
            _btnContinuar = Controls.Find("btnContinuar", true).FirstOrDefault();
            _btnEliminar = Controls.Find("btnEliminar", true).FirstOrDefault();

            if (_btnContinuar != null)
            {
                _btnContinuar.Click -= btnContinuar_Click;
                _btnContinuar.Click += btnContinuar_Click;

                if (_btnContinuar is IButtonControl ib) this.AcceptButton = ib;
            }

            // Asegurar que no queden como “opción”
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
            else WireOptionButtonsRecursive(this);

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

            if (EsAccion(root)) return;

            if (EsBotonOpcion(root))
            {
                root.Click -= Opcion_Click;
                root.Click += Opcion_Click;
            }
        }
        private bool EsAccion(Control c)
        {
            if (c == null) return false;
            if (ReferenceEquals(c, _btnContinuar) || ReferenceEquals(c, _btnEliminar)) return true;

            var n = (c.Name ?? "").ToLowerInvariant();
            if (n == "btncontinuar" || n == "btneliminar") return true;

            if (string.Equals(c.Tag as string, "accion", StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        private static bool EsBotonOpcion(Control c)
        {
            if (c == null) return false;
            if (c is Button) return true;
            var typeName = c.GetType().Name ?? "";
            if (typeName.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0) return true;
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
            if (CantidadRequerida > 0 && _seleccionados < CantidadRequerida)
            {
                MessageBox.Show($"Debes seleccionar {CantidadRequerida} tamal(es).",
                                "Tamales", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Asegura normalizar código a 10 dígitos por si alguna opción entró sin pad
            foreach (var s in Selecciones)
                s.Codigo = (s.Codigo ?? "").Trim().PadLeft(10, '0');

            this.DialogResult = DialogResult.OK;
            this.Close();
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

            // 1) Preferir Tag como código si el diseñador lo trae (ideal)
            string codigo = c.Tag as string;

            // 2) Si no hay Tag, inferir por texto (ajusta según tus botones)
            if (string.IsNullOrWhiteSpace(codigo))
            {
                var txt = desc.ToUpperInvariant();
                if (txt.Contains("CERDO")) codigo = COD_TAMAL_CERDO;
                else if (txt.Contains("POLLO")) codigo = COD_TAMAL_POLLO;
                else
                {
                    // fallback: por Name del control (NO recomendado si no es el código real)
                    codigo = (c.Name ?? "").Trim();
                }
            }

            // Normaliza a 10 dígitos
            codigo = (codigo ?? "").Trim().PadLeft(10, '0');

            return new SeleccionSimple
            {
                Codigo = codigo,
                Descripcion = desc,
                PrecioExtra = 0m,
                Notas = string.Empty
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
