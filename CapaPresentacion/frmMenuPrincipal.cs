using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using CapaDatos;
using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Administrador;
using CapaPresentacion.Ambientes;
using CapaPresentacion.Controles;
using CapaPresentacion.Helpers;
using CapaPresentacion.Impresion;
using CapaPresentacion.Notas;
using CapaPresentacion.Reportes;


namespace CapaPresentacion
{
    public partial class frmMenuPrincipal : Form
    {
        //Helper para bloquear eliminarcion
        //private ReadOnlyController _ro;
        private bool _soloLectura = false;

        private cnProducto _svcProductos;
        private readonly cnImpresora _cnImpresora = new cnImpresora();
        private readonly cnVendedor _cnVendedor = new cnVendedor();
        private readonly cnPedido _cnPed = new cnPedido();
        // Caché: CDG_PROD -> ceProductos (búsqueda O(1))
        private Dictionary<string, ceProductos> _cachePorCodigo;
        private Form _formHijoActual;
        private Form _categoriaActual;
        private Point? _dragStart = null;

        private DragScroller _lineasScroller;

        public int CantidadActualPublic => CantidadActual();
        private const bool CodigoSoloNumerico = true;

        private const int LARGO_CODIGO = 10; // ej. 0000001123

        private bool _pidioNumeroPersonas = false;
        //private const string COD_DESAYUNO_CONTINENTAL = "0000000457";

        private const string COD_DESAYUNO_CRIOLLO = "0000000458";
        private const string COD_DESAYUNO_PANERA = "0000000461";

        //private const string CHICHA_ALMUERZO_CODE = "0000000868";

        private int? _cantidadForzada;  // prioridad sobre txtCantidad cuando no es null
        //private bool _abriendoListaProductos = false;

        private bool _listaProductosAbierta = false;

        private bool _f2Bloqueado = false;

        // === PASTAS que disparan MenuPedidoItem ===
        private static readonly string[] COD_PASTAS =
        {
            "0000000273", // ALFREDO
            "0000000274", // HONGOS PORCÓN
            "0000000275", // PESTO CON MILANESA
            "0000000276"  // BOLOGNESA
        };
        private static bool EsCodigoPasta(string cod10) => COD_PASTAS.Contains((cod10 ?? "").Trim(), StringComparer.Ordinal);

        private sealed class UnidadJugo
        {
            public string Descripcion;   // ej. "JUGO DE PIÑA"
            public decimal PrecioExtra;  // recargo por esa unidad, si lo hubo
            public string Notas;         // texto libre: "SIN HELAR", etc.
        }

        public frmMenuPrincipal()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Load += frmMenuPrincipal_Load;

            //this.Shown += frmMenuPrincipal_Shown;
            this.KeyPreview = true;
            this.KeyDown += frmMenuPrincipal_KeyDown;

            _unlockF2Timer.Tick += (s, e) => { _unlockF2Timer.Stop(); _f2Bloqueado = false; };
        }

        public void MostrarEnCentral(Form form)
        {
            MostrarFormularioEnPanel(form, pnlCCentral);
        }
        private void MostrarFormularioEnPanel(Form formHijo, Panel panelHost)
        {

            foreach (Control ctrl in panelHost.Controls) ctrl.Dispose();
            panelHost.Controls.Clear();

            // Desengancha del anterior si lo había
            if (_formHijoActual is ISelectorProducto selOld)
                selOld.ProductoSeleccionado -= Hijo_ProductoSeleccionado;

            _formHijoActual = formHijo;

            formHijo.TopLevel = false;
            formHijo.FormBorderStyle = FormBorderStyle.None;
            formHijo.Dock = DockStyle.Fill;

            panelHost.Controls.Add(formHijo);
            formHijo.Show();
            formHijo.BringToFront();

            // 👇 Engancha si implementa ISelectorProducto (¡importante!)
            if (formHijo is ISelectorProducto sel)
                sel.ProductoSeleccionado += Hijo_ProductoSeleccionado;
        }

        //  private DragScroller _lineasScroller;

        private void frmMenuPrincipal_Load(object sender, EventArgs e)
        {
            // Cabecera
            txtAmb.Text = SesionActual.Ambiente ?? "";
            txtMesa.Text = SesionActual.Mesa?.Numero.ToString() ?? "";
            txtVendedor.Text = SesionActual.Vendedor?.Nombre ?? "";

            // Solo lectura visual
            txtAmb.ReadOnly = txtMesa.ReadOnly = txtVendedor.ReadOnly = true;

            // Servicios / cache
            _svcProductos = new cnProducto();
            RecargarCache("001");

            // Cantidad por defecto y validaciones
            txtCantidad.KeyPress += txtCantidad_KeyPress;
            if (string.IsNullOrWhiteSpace(txtCantidad.Text)) txtCantidad.Text = "1";

            // Botonera superior
            var sec = new frmBotoneraPrincipal(this);
            MostrarFormularioEnPanel(sec, pnlCSup);

            // ===== Lista de líneas (panel izquierdo) =====
            flpLineas.FlowDirection = FlowDirection.TopDown;  // vertical
            flpLineas.WrapContents = false;
            flpLineas.AutoScroll = true;

            // Arrastre + inercia + oculta barras (lo hace el scroller internamente)
            _lineasScroller = new CapaPresentacion.Helpers.DragScroller(
                flpLineas, CapaPresentacion.Helpers.DragAxis.Vertical);

            LineaSelection.Changed += (s, ev) =>
            {
                var sel = LineaSelection.Actual;
                if (sel == null)
                {
                    btnEliminar.Enabled = false;
                    btnComentarioLbr.Enabled = false;
                    return;
                }

                bool esBloqueada = _lineasBloqueadas.Contains(sel.View);

                // Eliminar sólo si NO es bloqueada
                //btnEliminar.Enabled = !esBloqueada;

                //// Comentario sólo si NO es bloqueada y el tipo lo permite
                //btnComentarioLbr.Enabled = !esBloqueada &&
                //    (sel is LineaPedidoItem || sel is ComboPedidoItem || sel is MenuPedidoItem);

                // DEBE QUEDAR ASÍ
                btnEliminar.Enabled = (sel != null); // ← siempre se puede intentar; la validación será en el Click
                btnComentarioLbr.Enabled = !esBloqueada &&
                    (sel is LineaPedidoItem || sel is ComboPedidoItem || sel is MenuPedidoItem);

            };

            // Estado inicial de botones (nada seleccionado)
            btnEliminar.Enabled = false;
            btnComentarioLbr.Enabled = false;

            // Recalcular total al agregar/quitar controles
            flpLineas.ControlAdded += (_, __) => ActualizarSubtotal();
            flpLineas.ControlRemoved += (_, __) => ActualizarSubtotal();

            chbNImpre.Checked = true;

        }

        private int CantidadActual()
        {
            //var p = TryParseCantidadCodigo(txtCantidad.Text);
            //return (p.ok && p.cantidad > 0) ? p.cantidad : 1;
            if (_cantidadForzada.HasValue)
            {
                int q = _cantidadForzada.Value;
                _cantidadForzada = null;          // se consume una vez
                return (q > 0) ? q : 1;
            }

            var p = TryParseCantidadCodigo(txtCantidad.Text);
            return (p.ok && p.cantidad > 0) ? p.cantidad : 1;
        }
        private void SeleccionarPorCodigoConCantidad(string codigo10, int cantidad)
        {
            _cantidadForzada = (cantidad > 0) ? cantidad : 1;  // la usará CantidadActual()
            Hijo_ProductoSeleccionado(codigo10);               // ← entra a TODO tu flujo central
        }

        private ceProductos BuscarProductoPorCodigoExacto(string codigo10)
        {
            if (string.IsNullOrWhiteSpace(codigo10)) return null;

            // 1) Caché en memoria
            if (_cachePorCodigo != null &&
                _cachePorCodigo.TryGetValue(codigo10.Trim(), out var pCache) &&
                pCache != null)
            {
                return pCache;
            }

            // 2) Capa de negocio
            var p = _svcProductos?.Obtener(codigo10.Trim(), "001");
            if (p != null)
            {
                // cachear para siguientes consultas
                _cachePorCodigo[codigo10.Trim()] = p;
            }
            return p;
        }

        // ---- BÚSQUEDA POR “TERMINA EN” (para cuando se escribe 214 en vez de 0000000214) ----
        private ceProductos BuscarProductoPorCodigoTerminaEn(string ultimosDigitos)
        {
            if (string.IsNullOrWhiteSpace(ultimosDigitos)) return null;
            ultimosDigitos = ultimosDigitos.Trim();

            // 1) Buscar en caché actual
            var enCache = (_cachePorCodigo?.Values ?? Enumerable.Empty<ceProductos>())
                          .Where(p => !string.IsNullOrEmpty(p.Codigo) &&
                                      p.Codigo.EndsWith(ultimosDigitos, StringComparison.Ordinal))
                          .ToList();

            if (enCache.Count == 1) return enCache[0];
            if (enCache.Count > 1) return enCache.OrderBy(p => p.Codigo).First();

            // 2) Si no hay en caché, pedir lista “básica” y filtrar
            var lista = _svcProductos?.ListarBasico("001") ?? new List<ceProductos>();
            var coincidencias = lista.Where(p => !string.IsNullOrEmpty(p.Codigo) &&
                                                 p.Codigo.EndsWith(ultimosDigitos, StringComparison.Ordinal))
                                     .ToList();

            if (coincidencias.Count == 1) return coincidencias[0];
            if (coincidencias.Count > 1) return coincidencias.OrderBy(p => p.Codigo).First();

            return null;
        }
        private void Hijo_ProductoSeleccionado(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return;

            string cod10 = codigo.Trim().PadLeft(LARGO_CODIGO, '0');

            // 1) Si es una de las PASTAS -> flujo MenuPedidoItem
            if (EsCodigoPasta(cod10))
            {
                if (!_cachePorCodigo.TryGetValue(cod10, out var prod) || prod == null)
                {
                    prod = _svcProductos.Obtener(cod10, "001");
                    if (prod != null) _cachePorCodigo[cod10] = prod;
                }
                if (prod == null)
                {
                    MessageBox.Show($"Producto {cod10} no encontrado.", "Pastas",
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int cantidad = CantidadActual();
                if (cantidad <= 0) cantidad = 1;

                AgregarMenuPasta(prod, cantidad);   // ← NBebidas + pinta MenuPedidoItem
                return;
            }

            // 2) Resto de productos (desayunos / helados / normales)
            ceProductos prod2;
            if (!_cachePorCodigo.TryGetValue(cod10, out prod2) || prod2 == null)
            {
                prod2 = _svcProductos.Obtener(cod10, "001");
                if (prod2 != null) _cachePorCodigo[cod10] = prod2;
            }
            if (prod2 == null)
            {
                MessageBox.Show($"Producto {cod10} no encontrado.", "Productos",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int cantidad2 = CantidadActual();

            if (_formHijoActual != null &&
            _formHijoActual.GetType().Name.Equals("frmBebidasC", StringComparison.OrdinalIgnoreCase))
            {
                if (cantidad2 <= 0) cantidad2 = 1;

                using (var frmN = new CapaPresentacion.Notas.frmNBebidas
                {
                    // Esto hace que arriba se vea: "1 x CHOCOLATE CALIENTE..."
                    ProductoBaseTexto = $"{cantidad2} x {prod2.Descripcion}",
                    TextoInicial = string.Empty
                })
                {
                    if (frmN.ShowDialog(this) == DialogResult.OK)
                    {
                        // Agrega la línea como en todos lados, con las notas del NBebidas
                        AgregarLineaPedido(prod2, cantidad2, frmN.Notas);
                    }
                }
                return;
            }
            if (_formHijoActual != null &&
                _formHijoActual.GetType().Name.Equals("frmAdicional", StringComparison.OrdinalIgnoreCase))
            {
                decimal pu = PrecioDe(prod2);   // tu helper existente (PRE_SOL / VAL_SOL)
                string notas = string.Empty;

                flpLineas.AddLinea(
                    prod2.Codigo,
                    prod2.Descripcion,
                    cantidad2,
                    pu,
                    notas,
                    seleccionar: false,         // 👈 antes estaba true; cámbialo a false
                    scrollIntoView: true
                );

                ActualizarSubtotal();           // si ya lo tienes implementado
                return;
            }
            if (_formHijoActual != null &&
                _formHijoActual.GetType().Name.StartsWith("frmSandwich", StringComparison.OrdinalIgnoreCase))
            {
                if (cantidad2 <= 0) cantidad2 = 1;

                string notas = string.Empty;
                using (var dlg = new CapaPresentacion.Notas.frmNSandwich
                {
                    ProductoBaseTexto = $"{cantidad2} x {prod2.Descripcion}",
                    TextoInicial = string.Empty
                })
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        notas = dlg.Notas ?? string.Empty;
                }


                // Agrega la línea normal del producto con las notas digitadas
                AgregarLineaPedido(prod2, cantidad2, notas);
                return;
            }
            if (_formHijoActual != null &&
                _formHijoActual.GetType().Name.IndexOf("Jugo", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (cantidad2 <= 0) cantidad2 = 1;

                string notas = string.Empty;
                using (var dlg = new CapaPresentacion.Notas.frmNBebidas
                {
                    ProductoBaseTexto = $"{cantidad2} x {prod2.Descripcion}",
                    TextoInicial = string.Empty
                })
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        notas = dlg.Notas ?? string.Empty;
                }

                // Agrega la línea con las notas digitadas
                AgregarLineaPedido(prod2, cantidad2, notas);
                return;
            }
            if (_formHijoActual != null &&
                _formHijoActual.GetType().Name.IndexOf("Waffle", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (cantidad2 <= 0) cantidad2 = 1;

                string notas = string.Empty;
                using (var dlg = new CapaPresentacion.Notas.frmNWaffles
                {
                    ProductoBaseTexto = $"{cantidad2} x {prod2.Descripcion}",
                    TextoInicial = string.Empty
                })
                {
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        notas = dlg.Notas ?? string.Empty;
                }

                AgregarLineaPedido(prod2, cantidad2, notas);
                return;
            }

            if (cantidad2 <= 0) cantidad2 = 1;

            SeleccionarProducto(prod2, cantidad2);
        }

        private void RecargarCache(string listaPrecio = "001")
        {
            var lista = _svcProductos.ListarBasico(listaPrecio);
            _cachePorCodigo = lista
                .Where(p => !string.IsNullOrWhiteSpace(p.Codigo))
                .GroupBy(p => p.Codigo.Trim())
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        }


        private void btnCerrar_Click(object sender, EventArgs e)
        {
            Close();
        }

        private (bool ok, int cantidad, string codigo) TryParseCantidadCodigo(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return (false, 1, null);

            input = input.Trim();

            var partes = input.Split('*');
            if (partes.Length == 1)
            {
                // Solo cantidad
                if (int.TryParse(partes[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) && n > 0)
                    return (true, n, null);

                return (false, 1, null);
            }
            else if (partes.Length == 2)
            {


                var qStr = partes[0].Trim();
                var codStr = partes[1].Trim();

                int q = 1;
                if (qStr.Length > 0 && (!int.TryParse(qStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out q) || q <= 0))
                    return (false, 1, null);

                if (CodigoSoloNumerico && !string.IsNullOrEmpty(codStr) && !codStr.All(char.IsDigit))
                    return (false, q, null);

                return (true, q, codStr);
            }

            return (false, 1, null);
        }

        private void btnComentarioLbr_Click(object sender, EventArgs e)
        {
            var sel = LineaSelection.Actual;
            if (sel == null)
            {
                MessageBox.Show("Selecciona primero un ítem.", "Comentario",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // 🚫 Si la línea está bloqueada (precargada de BD), no permitir editar comentario
            if (_lineasBloqueadas.Contains(sel.View))
            {
                try { System.Media.SystemSounds.Beep.Play(); } catch { /* opcional */ }
                return;
            }

            if (sel is LineaPedidoItem lp)
            {
                using (var dlg = new frmComentarioLbr())
                {
                    dlg.Texto = lp.Notas ?? string.Empty;   // o lp.GetNotasRaw() si lo prefieres
                    dlg.TextoInicial = dlg.Texto;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        lp.SetNotas(dlg.Comentario);
                }
            }
            else if (sel is ComboPedidoItem ci)
            {
                // 1) Si el usuario hizo click en el encabezado o mantiene SHIFT, edita encabezado
                if (ci.EncabezadoActivo || (ModifierKeys & Keys.Shift) == Keys.Shift)
                {
                    using (var dlg = new frmComentarioLbr())
                    {
                        dlg.Text = "Comentario del Combo";
                        dlg.Texto = ci.GetNotasEncabezadoRaw();
                        dlg.TextoInicial = dlg.Texto;
                        if (dlg.ShowDialog(this) == DialogResult.OK)
                            ci.SetNotasEncabezado(dlg.Comentario);
                    }
                    return;
                }

                // 2) Si hay subitems, intenta editarlos (si cancelas NO hace fallback a encabezado)
                if (ci.TieneSubItemEditable())
                {
                    ci.EditarUltimoJugoOBebida(this);
                    return;
                }

                // 3) Si no hay subitems, edita encabezado
                using (var dlg = new frmComentarioLbr())
                {
                    dlg.Text = "Comentario del Combo";
                    dlg.Texto = ci.GetNotasEncabezadoRaw();
                    dlg.TextoInicial = dlg.Texto;
                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        ci.SetNotasEncabezado(dlg.Comentario);
                }
            }
            else if (sel is MenuPedidoItem mi)
            {
                // Igual que ya lo tenías
                var zona = mi.ZonaActiva;
                if (zona == MenuPedidoItem.ZonaNotas.Ninguna)
                    zona = MenuPedidoItem.ZonaNotas.Chicha;

                using (var dlg = new frmComentarioLbr())
                {
                    dlg.Texto = mi.GetNotasRaw(zona);
                    dlg.TextoInicial = dlg.Texto;
                    dlg.Text = (zona == MenuPedidoItem.ZonaNotas.Menu)
                               ? "Comentario del Menú"
                               : "Comentario de la Chicha";

                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        mi.SetNotas(zona, dlg.Comentario);
                }
            }
        }


        private void txtCantidad_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;
            var parsed = TryParseCantidadCodigo(txtCantidad.Text);
            if (!parsed.ok)
            {
                MessageBox.Show("Formato inválido. Usa: cantidad o cantidad*código");
                e.SuppressKeyPress = true;
                return;
            }

            // Si solo hay cantidad, no buscamos aún
            if (parsed.codigo == null)
            {
                e.SuppressKeyPress = true;
                return;
            }

            // Normaliza el código si es numérico
            string codIngresado = parsed.codigo.Trim();
            string cod10 = codIngresado.All(char.IsDigit)
                ? codIngresado.PadLeft(LARGO_CODIGO, '0')
                : codIngresado;

            // Buscar: exacto -> termina en
            var producto = BuscarProductoPorCodigoExacto(cod10);
            if (producto == null && codIngresado.All(char.IsDigit))
                producto = BuscarProductoPorCodigoTerminaEn(codIngresado);

            if (producto == null)
            {
                MessageBox.Show($"No se encontró el producto '{codIngresado}'.");
                e.SuppressKeyPress = true;
                return;
            }

            //  Unificar el flujo: SOLO esta llamada
            SeleccionarPorCodigoConCantidad(producto.Codigo, parsed.cantidad);

            // Limpieza
            txtCantidad.Clear();
            txtCantidad.Focus();
            e.SuppressKeyPress = true;
        }
        private void SeleccionarProducto(ceProductos prod, int cantidad)
        {
            if (prod == null || cantidad <= 0) return;

            string cod10 = (prod.Codigo ?? "").Trim().PadLeft(10, '0');

            // Desayunos con T A M A L (la diferencia es solo cuántos tamales por unidad)
            if (cod10 == COD_DESAYUNO_CRIOLLO)
            {
                EjecutarWizardDesayunoConTamal(prod, cantidad, tamalesPorUnidad: 1);
                return;
            }
            if (cod10 == COD_DESAYUNO_PANERA)
            {
                EjecutarWizardDesayunoConTamal(prod, cantidad, tamalesPorUnidad: 2);
                return;
            }

            // Resto de combos (sin tamales): jugo x unidad + bebidas calientes para el total
            if (_svcProductos.EsComboDesayuno(prod))
            {
                EjecutarWizardDesayunoPorUnidad(prod, cantidad);
                return;
            }

            // Flujo normal (helados, etc.)
            if (RequiereNotas(cod10))
            {
                using (var dlg = new CapaPresentacion.Notas.frmNHelados())
                {
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.Cantidad = cantidad;
                    dlg.Producto = prod.Descripcion ?? prod.Codigo;

                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        AgregarLineaPedido(prod, cantidad, dlg.Notas);
                }
            }
            else
            {
                AgregarLineaPedido(prod, cantidad, string.Empty);
            }
        }
        private void txtCantidad_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;

            string text = txtCantidad.Text;
            int caret = txtCantidad.SelectionStart;
            bool tieneAsterisco = text.Contains('*');

            if (e.KeyChar == '*')
            {
                if (tieneAsterisco || (caret == 0 && txtCantidad.SelectionLength == 0))
                    e.Handled = true;
                return;
            }

            if (!tieneAsterisco) { if (!char.IsDigit(e.KeyChar)) e.Handled = true; return; }
            if (!char.IsDigit(e.KeyChar)) e.Handled = true; // código numérico
        }
        private void AbrirListaProductos()
        {
            if (_listaProductosAbierta) return;     // evita reentrar / abrir doble
            _listaProductosAbierta = true;
            try
            {
                using (var lstprd = new frmListaProductos())
                {
                    lstprd.StartPosition = FormStartPosition.CenterParent;
                    var r = lstprd.ShowDialog(this);
                    if (r == DialogResult.OK && !string.IsNullOrWhiteSpace(lstprd.SelectedCodigo))
                        Hijo_ProductoSeleccionado(lstprd.SelectedCodigo);
                }
            }
            finally
            {
                _listaProductosAbierta = false;
            }
        }
        private void btnListarProductos_Click(object sender, EventArgs e)
        {
            AbrirListaProductos();
        }

        // Códigos que requieren el diálogo de notas (puedes agregar más a futuro)
        private static readonly HashSet<string> _codigosConNotas =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "0000000583", // HELADO DE 2 BOLAS
                "0000000584"  // HELADO DE 1 BOLA
            };

        private bool RequiereNotas(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo)) return false;
            // Asegura 10 dígitos si en tus botones llega sin ceros a la izquierda.
            var cod10 = codigo.Trim().PadLeft(10, '0');
            return _codigosConNotas.Contains(cod10);
        }

        private static decimal PrecioDe(ceProductos p)
        {
            return p.PrecioUnitario != 0 ? p.PrecioUnitario : p.ValorUnitario;
        }

        // private static decimal PrecioDe(ceProductos p) => (p?.PrecioUnitario ?? 0m) != 0m ? p.PrecioUnitario : (p?.ValorUnitario ?? 0m);

        private void AgregarLineaPedido(ceProductos prod, int cantidad, string notas)
        {


            if (prod == null || cantidad <= 0) return;

            decimal pu = PrecioDe(prod);

            var item = new LineaPedidoItem();
            item.Configurar(prod.Codigo, prod.Descripcion, cantidad, pu, notas ?? string.Empty);

            flpLineas.SuspendLayout();
            flpLineas.Controls.Add(item);
            flpLineas.ResumeLayout();

            // 🔸 Selecciona globalmente la línea recién agregada y hace scroll hacia ella
            LineaSelection.Select(item, true);

            // Los botones se actualizan solos por el handler de LineaSelection.Changed
            ActualizarSubtotal();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {

            // Selección actual
            var sel = LineaSelection.Actual;
            if (sel == null)
            {
                try { System.Media.SystemSounds.Beep.Play(); } catch { }
                return;
            }

            var view = sel.View;
            var parent = view?.Parent as Control;
            if (parent == null) return;

            // Si la línea viene de BD (bloqueada), solo permitir en reingreso y con clave de supervisor
            if (_lineasBloqueadas.Contains(view))
            {
                // En primer ingreso nunca hay líneas antiguas; si las hubiera, no permitir
                if (!_esReingresoMesa)
                {
                    try { System.Media.SystemSounds.Beep.Play(); } catch { }
                    return;
                }

                // Validación supervisor (por usuario) antes de permitir borrar
                if (!PermiteEliminarAhora())
                    return;
            }

            // Ejecuta la eliminación: se encarga de borrar en BD (NUM_ITEM/CDG_FPRD),
            // recalcular totales y quitar el control de la UI si corresponde.
            EjecutarEliminacionSeleccionActual();
        }

        private ILineaSeleccionable BuscarSeleccionableVecino(Control parent, int removedIndex)
        {
            if (parent == null || parent.Controls.Count == 0) return null;

            // 1) mismo índice (si quedó algo ahí)
            int start = Math.Min(removedIndex, parent.Controls.Count - 1);
            if (start >= 0 && parent.Controls[start] is ILineaSeleccionable s1) return s1;

            // 2) hacia atrás
            for (int i = start - 1; i >= 0; i--)
                if (parent.Controls[i] is ILineaSeleccionable s2) return s2;

            // 3) hacia adelante (por si acaso)
            for (int i = start + 1; i < parent.Controls.Count; i++)
                if (parent.Controls[i] is ILineaSeleccionable s3) return s3;

            return null;
        }
        //private void frmMenuPrincipal_Shown(object sender, EventArgs e)
        //{
        //    if (_pidioNumeroPersonas) return;
        //    _pidioNumeroPersonas = true;

        //    using (var dlg = new frmNumeroPersonas())
        //    {
        //        var r = dlg.ShowDialog(this);
        //        if (r != DialogResult.OK)
        //        {
        //            // Si cancelan, cierra el principal (o decide qué comportamiento quieres)
        //            Close();
        //            return;
        //        }
        //        // Copia la cantidad al textbox del principal
        //        txtNPersonas.Text = dlg.Cantidad.ToString();
        //    }
        //}

        // Para transportar lo que el usuario escogió en cada paso del wizard
        private sealed class SeleccionSimple
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioExtra { get; set; } // 0 si no aplica
        }
        private void ActualizarSubtotal()
        {
            decimal total = 0m;

            foreach (Control c in flpLineas.Controls)
            {
                if (c is CapaPresentacion.Controles.LineaPedidoItem li)
                    total += li.Importe;

                else if (c is CapaPresentacion.Controles.ComboPedidoItem ci)
                    total += ci.Total;

                else if (c is CapaPresentacion.Controles.MenuPedidoItem mi)   // 👈 NUEVO
                    total += mi.Total;
            }

            txtSubtotal.Text = $"S/ {total:0.00}";
        }


        private void EjecutarWizardDesayunoConTamal(ceProductos prod, int cantidad, int tamalesPorUnidad)
        {
            if (prod == null || cantidad <= 0) return;

            var item = new CapaPresentacion.Controles.ComboPedidoItem
            {
                AgruparJugosIguales = true,
                AgruparBebidasIguales = true,
                AgruparTamalesIguales = true
            };

            // =======================
            // 1) J U G O  (1 por unidad) + notas del jugo
            // =======================
            int calientesPendientes = cantidad; // 1 caliente por desayuno

            for (int i = 1; i <= cantidad; i++)
            {
                // Elegir jugo de ESTE desayuno
                List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> sel;
                using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
                {
                    frmJ.CantidadRequerida = 1;     // siempre 1 por desayuno
                    frmJ.ListaPrecio = "001";
                    frmJ.ProductoBaseTexto = $"1 x {prod.Descripcion}  ({i}/{cantidad})";

                    if (frmJ.ShowDialog(this) != DialogResult.OK) return;
                    sel = frmJ.Selecciones ?? new List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
                    if (sel.Count == 0) return;
                }

                var jugo = sel[0];

                // Notas rápidas del jugo (y posible “GRANDE” que consume 1 caliente)
                string notasJugo = string.Empty;
                using (var frmN = new CapaPresentacion.Notas.frmNBebidas())
                {
                    if (frmN.ShowDialog(this) == DialogResult.OK)
                    {
                        notasJugo = frmN.Notas ?? string.Empty;
                        if (frmN.CuposCalienteConsumidos > 0 && calientesPendientes > 0)
                            calientesPendientes -= 1;   // “GRANDE” descuenta 1 caliente
                    }
                }

                // Agregar jugo (sub-línea con PU 0.00; el recargo queda en el PU final del combo)
                item.AddJugoUnidad(
                    codigo: TryGet<string>(jugo, "Codigo", ""),
                    descripcion: TryGet<string>(jugo, "Descripcion", "JUGO"),
                    precioExtra: TryGet<decimal>(jugo, "PrecioExtra", 0m),
                    notas: notasJugo,
                    forzarIndividual: null
                );
            }

            // =======================
            // 2) B E B I D A  C A L I E N T E  (si quedaron cupos)
            // =======================
            if (calientesPendientes > 0)
            {
                using (var frmB = new CapaPresentacion.Notas.frmCBebidasCalientes
                {
                    CantidadRequerida = calientesPendientes,
                    ListaPrecio = "001",
                    ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}",
                    ReglaAdicionalLecheActiva = true   // si la usas
                })
                {
                    if (frmB.ShowDialog(this) == DialogResult.OK)
                    {
                        var sels = frmB.Selecciones ?? new List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple>();
                        foreach (var b in sels)
                        {
                            item.AddBebidaUnidad(
                                codigo: TryGet<string>(b, "Codigo", ""),
                                descripcion: TryGet<string>(b, "Descripcion", "BEBIDA"),
                                precioExtra: TryGet<decimal>(b, "PrecioExtra", 0m),
                                notas: TryGet<string>(b, "Notas", ""),
                                forzarIndividual: false
                            );
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // =======================
            // 3) T A M A L E S  (con CÓDIGO, PU = 0.00)
            //    totalTamales = tamalesPorUnidad * cantidad
            // =======================
            int totalTamales = tamalesPorUnidad * cantidad;
            if (totalTamales > 0)
            {
                using (var frmT = new CapaPresentacion.Notas.frmCDesayunoTamal())
                {
                    frmT.CantidadRequerida = totalTamales;
                    frmT.ListaPrecio = "001";
                    frmT.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}";

                    if (frmT.ShowDialog(this) != DialogResult.OK) return;

                    var sels = frmT.Selecciones ?? new List<CapaPresentacion.Notas.frmCDesayunoTamal.SeleccionSimple>();
                    foreach (var t in sels)
                    {
                        // 👇 IMPORTANTÍSIMO: usa el CÓDIGO del tamal
                        string codTam = (t.Codigo ?? "").Trim().PadLeft(10, '0');
                        string desTam = t.Descripcion ?? "TAMAL";
                        decimal pxTam = t.PrecioExtra;   // normalmente 0

                        // AddTamalUnidad(codigo, descripcion, precio, notas, forzarIndividual)
                        item.AddTamalUnidad(codTam, desTam, pxTam, /*notas*/ string.Empty, /*forzarIndividual*/ null);
                    }
                }
            }

            // =======================
            // 4) Precio del combo = base + (promedio extras jugo/bebida)
            //    (los tamales no suelen tener extra; si lo tuvieran, ya se promedió arriba)
            // =======================
            decimal puBase = PrecioDe(prod); // CON IGV
            decimal puFinal = puBase + item.GetExtraPromedioTotalPorUnidad(cantidad);

            // =======================
            // 5) Pintar combo
            // =======================
            item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puFinal);

            flpLineas.SuspendLayout();
            flpLineas.Controls.Add(item);
            flpLineas.ResumeLayout();

            LineaSelection.Select(item, true);
            btnEliminar.Enabled = (LineaSelection.Actual != null);
            btnComentarioLbr.Enabled = (LineaSelection.Actual != null);

            ActualizarSubtotal();
        }



        private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        {
            if (prod == null || cantidad <= 0) return;

            string notasEncabezado = string.Empty;
            string cod10 = (prod.Codigo ?? "").Trim().PadLeft(10, '0');

            // Si necesitas un pre-notas para algún combo específico, aquí:
            // (ejemplo ya lo tenías con Continental)
            if (cod10 == "0000000457") // COD_DESAYUNO_CONTINENTAL
            {
                using (var pre = new CapaPresentacion.Notas.frmNDesayunoContinental { ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}" })
                {
                    if (pre.ShowDialog(this) != DialogResult.OK) return;
                    notasEncabezado = pre.Notas ?? string.Empty;
                }
            }

            var item = new CapaPresentacion.Controles.ComboPedidoItem
            {
                AgruparJugosIguales = true,
                AgruparBebidasIguales = true
            };

            int calientesPendientes = cantidad;

            for (int i = 1; i <= cantidad; i++)
            {
                // === Jugo ===
                object jugoSel;
                using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
                {
                    frmJ.CantidadRequerida = 1;
                    frmJ.ListaPrecio = "001";
                    frmJ.ProductoBaseTexto = $"1 x {prod.Descripcion} ({i}/{cantidad})";

                    if (frmJ.ShowDialog(this) != DialogResult.OK) return;
                    var lista = frmJ.Selecciones ?? new System.Collections.Generic.List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
                    if (lista.Count == 0) return;
                    jugoSel = lista[0];
                }

                string codJugo = TryGet<string>(jugoSel, "Codigo", "");
                string desJugo = TryGet<string>(jugoSel, "Descripcion", "JUGO");
                decimal pxJugo = TryGet<decimal>(jugoSel, "PrecioExtra", 0m);

                // === Notas del jugo ===
                string notasJugo = string.Empty;
                using (var frmN = new CapaPresentacion.Notas.frmNBebidas { ProductoBaseTexto = desJugo })
                {
                    if (frmN.ShowDialog(this) == DialogResult.OK)
                    {
                        notasJugo = frmN.Notas ?? string.Empty;
                        if (frmN.CuposCalienteConsumidos > 0 && calientesPendientes > 0)
                            calientesPendientes -= 1;
                    }
                }

                // === Agregar al control (y exportar) ===
                item.AddJugoUnidad(codJugo, desJugo, pxJugo, notasJugo, null);
            }

            // === Bebidas calientes (si quedaron) ===
            if (calientesPendientes > 0)
            {
                using (var frmB = new CapaPresentacion.Notas.frmCBebidasCalientes
                {
                    CantidadRequerida = calientesPendientes,
                    ListaPrecio = "001",
                    ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}",
                    ReglaAdicionalLecheActiva = true
                })
                {
                    if (frmB.ShowDialog(this) == DialogResult.OK)
                    {
                        var sels = frmB.Selecciones ?? new System.Collections.Generic.List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple>();
                        foreach (var b in sels)
                        {
                            string codB = TryGet<string>(b, "Codigo", "");
                            string desB = TryGet<string>(b, "Descripcion", "BEBIDA");
                            decimal pxB = TryGet<decimal>(b, "PrecioExtra", 0m);
                            string notasB = TryGet<string>(b, "Notas", "");
                            item.AddBebidaUnidad(codB, desB, pxB, notasB, false);
                        }
                    }
                }
            }

            // === Precio final = base + promedio de extras ===
            decimal puBase = PrecioDe(prod);
            decimal puFinal = puBase + item.GetExtraPromedioTotalPorUnidad(cantidad);

            item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puFinal);
            if (!string.IsNullOrWhiteSpace(notasEncabezado))
                item.AppendNotasEncabezado(notasEncabezado);

            // Pinta en la UI
            flpLineas.SuspendLayout();
            flpLineas.Controls.Add(item);
            flpLineas.ResumeLayout();

            LineaSelection.Select(item, true);
            btnEliminar.Enabled = (LineaSelection.Actual != null);
        }

        private void AgregarMenuPasta(ceProductos menuProd, int cantidad)
        {
            if (menuProd == null || cantidad <= 0) return;

            // 1) Resolver chicha con código
            const string CHICHA_ALMUERZO_CODE = "0000000868";
            var chicha = _svcProductos.Obtener(CHICHA_ALMUERZO_CODE, "001")
                         ?? new ceProductos { Codigo = CHICHA_ALMUERZO_CODE, Descripcion = "CHICHA ALMUERZO", PrecioUnitario = 0m };

            // 2) Notas de la chicha (NBebidas)
            string notasChicha = string.Empty;
            using (var frm = new CapaPresentacion.Notas.frmNBebidas())
            {
                frm.ProductoBaseTexto = $"{cantidad} x {chicha.Descripcion}";
                frm.TextoInicial = string.Empty;

                if (frm.ShowDialog(this) != DialogResult.OK) return;
                notasChicha = frm.Notas ?? string.Empty;
            }

            // 3) Crear y poblar el control
            var item = new MenuPedidoItem();

            decimal puMenu = PrecioDe(menuProd); // CON IGV (PRE_SOL)
            item.SetMenu(menuProd.Codigo, menuProd.Descripcion, cantidad, puMenu);

            // SetChicha con CÓDIGO (y notas)
            item.SetChicha(chicha.Codigo, chicha.Descripcion, notasChicha, cantidad);

            // 4) Insertar en el panel
            AjustarAnchoItem(item);
            flpLineas.SuspendLayout();
            flpLineas.Controls.Add(item);
            flpLineas.ResumeLayout(true);

            // 5) Selección y total
            LineaSelection.Select(item, true);
            btnEliminar.Enabled = btnComentarioLbr.Enabled = (LineaSelection.Actual != null);
            ActualizarSubtotal();
        }
        private void AjustarAnchoItem(Control item)
        {
            if (item == null || flpLineas == null) return;

            int usable = flpLineas.ClientSize.Width
                       - flpLineas.Padding.Left
                       - flpLineas.Padding.Right
                       - item.Margin.Horizontal;

            if (usable < 80) usable = 80;
            item.Width = usable;
        }
        private string ResolverImpresoraPorProducto(string codigo10)
        {
            // 1) Cache en memoria
            if (_cachePorCodigo != null &&
                _cachePorCodigo.TryGetValue((codigo10 ?? "").Trim(), out var pCache) &&
                pCache != null && !string.IsNullOrWhiteSpace(pCache.IMP_PROD))
                return pCache.IMP_PROD;

            // 2) Servicio (si no estaba en cache)
            var p = _svcProductos?.Obtener((codigo10 ?? "").Trim(), "001");
            if (p != null)
            {
                _cachePorCodigo[codigo10.Trim()] = p;           // refresca cache
                if (!string.IsNullOrWhiteSpace(p.IMP_PROD))
                    return p.IMP_PROD;
            }

            return string.Empty; // si no hay dato en maestro
        }
        private void btnActualizar_Click(object sender, EventArgs e)
        {
            try
            {
                var todos = flpLineas.Controls.Cast<Control>().ToList();
                var nuevos = todos.Where(c => !_lineasBloqueadas.Contains(c)).ToList();
                var viejos = todos.Where(c => _lineasBloqueadas.Contains(c)).ToList();

                if (nuevos.Count == 0)
                {
                    MessageBox.Show("No hay ítems nuevos para guardar.", "Actualizar",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                btnActualizar.Enabled = btnEliminar.Enabled = btnComentarioLbr.Enabled = false;
                Cursor = Cursors.WaitCursor;

                // Datos cabecera
                string cdgVend = SesionActual.Vendedor?.Codigo ?? "";
                string cdgUsr = SesionActual.Usuario ?? "";
                string cdgLoc = SesionActual.Local ?? "000";
                string cdgCaja = SesionActual.Caja ?? "";
                string numMesa = (txtMesa.Text ?? "").Trim();
                int numPers = int.TryParse(txtNPersonas.Text, out var np) ? np : 0;

                Func<string, string> resolverImpresora = ResolverImpresoraPorProducto;

                // Resolución de tributos desde el cache de productos
                Func<string, (decimal?, bool?)?> resolverTribVT = (cod10) =>
                {
                    try
                    {
                        if (_cachePorCodigo != null &&
                            _cachePorCodigo.TryGetValue((cod10 ?? "").Trim(), out ceProductos p) &&
                            p != null)
                        {
                            decimal porIgv = TryGet<decimal>(p, "POR_IGV", -1m);
                            bool swtIgv = TryGet<bool>(p, "SWT_IGV", TryGet<bool>(p, "AFECTO_IGV", false));
                            bool hasPor = (porIgv >= 0m);
                            if (hasPor || swtIgv)
                                return (hasPor ? (decimal?)porIgv : null, (bool?)swtIgv);
                        }
                    }
                    catch { }
                    return null;
                };
                Func<string, Tuple<decimal?, bool?>> resolverTribTuple =
                    cod => { var r = resolverTribVT(cod); return r.HasValue ? Tuple.Create(r.Value.Item1, r.Value.Item2) : null; };

                var svc = new CapaNegocio.cnPedido();

                // ======================================================
                // CASO A: ya existe NUM_PED abierto -> ANEXAR SOLO NUEVOS
                // ======================================================
                if (!string.IsNullOrWhiteSpace(_numPedActual))
                {
                    // Generar ceMPedido/ceDPedido SOLO desde los controles nuevos
                    var resNuevos = TxtPedidoWriter.Generar(
                        controles: nuevos,
                        resolverImpresora: resolverImpresora,
                        cdgVend: cdgVend,
                        cdgUsr: cdgUsr,
                        cdgLoc: cdgLoc,
                        cdgCaja: cdgCaja,
                        numMesa: numMesa,
                        numPers: numPers,
                        resolverTrib: cod => resolverTribVT(cod)
                    );

                    // ❗ Corregido: usa argumento posicional (o el nombre correcto, p.ej. numPed8)
                    svc.AnexarSoloDetalles(
                        _numPedActual,
                        resNuevos.Cabecera.Detalles,
                        resolverImpresora,
                        resolverTribTuple
                    );

                    // Imprimir SOLO lo nuevo
                    if (chbNImpre.Checked)
                        ImprimirImpresoras(
                            new ceMPedido
                            {
                                NUM_PED = _numPedActual,
                                CDG_AMB = SesionActual.Ambiente,
                                FEC_PED = DateTime.Now,
                                NUM_PERS = numPers,
                                CDG_VEND = cdgVend,
                                NUM_MESA = numMesa
                            },
                            resNuevos.Cabecera.Detalles
                        );

                    // Marcar visualmente como bloqueados para no re-enviar
                    foreach (var c in nuevos) MarcarSoloLectura(c);

                    MessageBox.Show($"Ítems nuevos anexados al pedido {_numPedActual}.",
                        "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // ======================================================
                // CASO B: NO hay pedido abierto -> CREAR NUEVO con TODO
                // ======================================================
                var resTodos = TxtPedidoWriter.Generar(
                    controles: todos,
                    resolverImpresora: resolverImpresora,
                    cdgVend: cdgVend,
                    cdgUsr: cdgUsr,
                    cdgLoc: cdgLoc,
                    cdgCaja: cdgCaja,
                    numMesa: numMesa,
                    numPers: numPers,
                    resolverTrib: cod => resolverTribVT(cod)
                );

                // Guardar en BD (crea nuevo NUM_PED)
                string numPedAsignado = svc.GuardarDb(resTodos.Cabecera, resolverImpresora, resolverTribTuple);
                if (!string.IsNullOrWhiteSpace(numPedAsignado))
                {
                    resTodos.Cabecera.NUM_PED = numPedAsignado;
                    _numPedActual = numPedAsignado;
                }

                if (chbNImpre.Checked)
                    ImprimirImpresoras(resTodos.Cabecera, resTodos.Cabecera.Detalles);

                foreach (var c in todos) MarcarSoloLectura(c);

                MessageBox.Show(
                    "Pedido creado en BD.\n\n" +
                    "NUM_PED : " + resTodos.Cabecera.NUM_PED + "\n" +
                    "Items   : " + resTodos.CantItems + "\n" +
                    "SubTotal: S/ " + resTodos.Cabecera.IMP_BASE.ToString("0.00") + "\n" +
                    "IGV     : S/ " + resTodos.Cabecera.IMP_IGV.ToString("0.00") + "\n" +
                    "Total   : S/ " + resTodos.Cabecera.IMP_TOT.ToString("0.00"),
                    "OK", MessageBoxButtons.OK, MessageBoxIcon.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar el pedido: " + ex.Message,
                    "Actualizar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
                btnActualizar.Enabled = true;

                var sel = LineaSelection.Actual;
                bool bloqueada = (sel != null) && _lineasBloqueadas.Contains(sel.View);
                btnEliminar.Enabled = (sel != null) && !bloqueada;
                btnComentarioLbr.Enabled = (sel != null) && !bloqueada;
            }

             this.Close(); // si quisieras cerrar al terminar
        }
        private void ImprimirImpresoras(ceMPedido cab, IList<ceDPedido> dets)
        {
            if (cab == null || dets == null || dets.Count == 0) return;

            // 1) agrupar por destino (toma IMP_PROD y CDG_IMP de cada producto)
            var porDestino = _cnImpresora.AgruparPorDestino(dets.ToList());

            var cacheNom = new Dictionary<int, string>();
            foreach (var kv in porDestino)
            {
                string cdgForm = kv.Key;      // "001", "003", etc.
                var detsDestino = kv.Value;    // solo líneas para ese destino

                // 2) construir DTO
                var t = new ComandaTicket
                {
                    Ambiente = ResolverAmbienteTexto(cab.CDG_AMB),
                    FechaHora = (cab.FEC_PED == default(DateTime)) ? DateTime.Now : cab.FEC_PED,
                    NroPedido = (cab.NUM_PED ?? "").PadLeft(8, '0'),
                    NroPersonas = cab.NUM_PERS,
                    //Vendedor = cnVendedor.ObtenerNombre(cab.CDG_VEND) ?? cab.CDG_VEND,
                    Vendedor = _cnVendedor.ObtenerNombre(cab.CDG_VEND) ?? cab.CDG_VEND,
                    Mesa = (cab.NUM_MESA ?? "").PadLeft(3, '0')
                };

                foreach (var d in detsDestino)
                {

                    string nom;
                    if (!cacheNom.TryGetValue(d.CDG_PROD, out nom))
                    {
                        nom = DAOProductos.ObtenerDescripcion(d.CDG_PROD);
                        if (string.IsNullOrEmpty(nom))
                            nom = !string.IsNullOrEmpty(d.COD10) ? d.COD10 : d.CDG_PROD.ToString();
                        cacheNom[d.CDG_PROD] = nom;
                    }

                    // 🔹 Mostrar solo lo que digitó el mozo, sin el tag [#...#]
                    string obsOriginal = d.OBS_PPRD ?? string.Empty;
                    string obsVisible;
                    TryParseTag(obsOriginal, out obsVisible);   // descarta el tag y deja el texto visible

                    // (opcional) quita un guion inicial estético si lo usas: "- SIN HIELO"
                    if (!string.IsNullOrWhiteSpace(obsVisible))
                        obsVisible = obsVisible.TrimStart(' ', '-').Trim();

                    t.Lineas.Add(new ComandaTicket.Linea
                    {
                        Cantidad = d.CAN_PPRD,
                        NombreProducto = nom,
                        Notas = obsVisible                  // 👈 ya sin tags
                    });
                }

                // 3) etiqueta visible en papel (opcional)
                string etiquetaDestino;
                switch (cdgForm)
                {
                    case "001": etiquetaDestino = "PASTELERÍA - HELADERÍA"; break;
                    case "002": etiquetaDestino = "TICKET PRE-CUENTA"; break;
                    case "003": etiquetaDestino = "COCINA"; break;
                    case "004": etiquetaDestino = "JUGUERÍA"; break;
                    default: etiquetaDestino = cdgForm; break;
                }

                string texto = TicketRenderer.Render(t, etiquetaDestino);

                // 4) resolver nombre de impresora Windows y enviar
                string impresoraWin = _cnImpresora.ResolverNombreImpresora(cdgForm);
                if (string.IsNullOrWhiteSpace(impresoraWin))
                {
                    MessageBox.Show("No hay impresora configurada para el formato " + cdgForm + ".",
                        "Impresión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                EscPosPrinter.Print(impresoraWin, texto, true, false);
            }
        }
        // helper simple (si ya tienes uno, usa el tuyo)
        private string ResolverAmbienteTexto(string cdgAmb)
        {
            return string.IsNullOrWhiteSpace(cdgAmb) ? "" : cdgAmb;
        }

        private static T TryGet<T>(object obj, string prop, T def)
        {
            if (obj == null) return def;
            var p = obj.GetType().GetProperty(prop, BindingFlags.Public | BindingFlags.Instance);
            if (p == null) return def;
            try
            {
                var v = p.GetValue(obj, null);
                if (v == null) return def;
                return (T)Convert.ChangeType(v, typeof(T), CultureInfo.InvariantCulture);
            }
            catch { return def; }
        }
        // private DateTime _lastF2 = DateTime.MinValue;
        private readonly Timer _unlockF2Timer = new Timer { Interval = 200 };
        private void frmMenuPrincipal_KeyDown(object sender, KeyEventArgs e)
        {

            //// Supr: solo si la línea seleccionada NO es bloqueada
            //if (e.KeyCode == Keys.Delete)
            //{
            //    var sel = LineaSelection.Actual;
            //    if (sel != null && _lineasBloqueadas.Contains(sel.View))
            //    {
            //        e.SuppressKeyPress = true;
            //        return; // no borra
            //    }
            //}
            if (e.KeyCode == Keys.Delete)
            {
                e.SuppressKeyPress = true;
                btnEliminar.PerformClick();   // deja que el handler pida CDG_USR si hace falta
                return;
            }

            // F2: siempre permitido (para agregar más productos)
            if (e.KeyCode == Keys.F2 && !_f2Bloqueado)
            {
                _f2Bloqueado = true;
                e.SuppressKeyPress = true;
                AbrirListaProductos();
                _unlockF2Timer.Start();
            }

            // Ctrl+E (comentario) – bloquear si es línea RO
            if (e.Control && e.KeyCode == Keys.E)
            {
                var sel = LineaSelection.Actual;
                if (sel != null && _lineasBloqueadas.Contains(sel.View))
                {
                    e.SuppressKeyPress = true;
                    return;
                }
                // si no está bloqueada, dejas fluir a tu handler
            }
        }
        private string _numPedActual = null;  // opcional, por si quieres guardarlo en el form
        private void frmMenuPrincipal_KeyUp(object sender, KeyEventArgs e)
        {
        }
        private bool _hayPedidoAbierto = false;
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // 1) Intentar precargar (esto setea _hayPedidoAbierto si aplica)
            PrecargarPedidoAbiertoDeMesa();

            // 2) Si ya hay pedido, no pidas número de personas otra vez
            if (_hayPedidoAbierto) return;

            // === flujo normal (sin pedido previo) ===
            if (_pidioNumeroPersonas) return;
            _pidioNumeroPersonas = true;

            using (var dlg = new frmNumeroPersonas())
            {
                var r = dlg.ShowDialog(this);
                if (r != DialogResult.OK)
                {
                    Close();
                    return;
                }
                txtNPersonas.Text = dlg.Cantidad.ToString();
            }
        }
        // === Tag de OBS_PPRD ===
        private sealed class TagInfo
        {
            public string Tipo;    // HEAD, JUG, BEB, TAM, CHI
            public string Id;      // C=...
            public string Cod;     // COD=...
            public string Desc;    // D=...
            public int? Q;      // Q= (solo HEAD)
            public decimal? Pu;    // PU= (solo HEAD, CON IGV)
            public string Notas;   // N=
        }


        private static readonly System.Text.RegularExpressions.Regex _rxTag =
        new System.Text.RegularExpressions.Regex(
            @"\[#T=(?<t>HEAD|MENU|JUG|BEB|TAM|CHI|CHICHA);C=(?<c>[^;#]+)(?:;COD=(?<cod>[^;#]+))?(?:;D=(?<d>[^;#]+))?(?:;Q=(?<q>\d+))?(?:;PU=(?<pu>\d+(?:\.\d+)?))?(?:;N=(?<n>[^#]*))?#\]",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant
        );


        private static TagInfo TryParseTag(string obs, out string obsLimpia)
        {
            obsLimpia = (obs ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(obsLimpia)) return null;

            var m = _rxTag.Match(obsLimpia);
            if (!m.Success) return null;

            var ti = new TagInfo
            {
                Tipo = (m.Groups["t"].Value ?? "").Trim().ToUpperInvariant(),
                Id = (m.Groups["c"].Value ?? "").Trim(),
                Cod = (m.Groups["cod"].Success ? m.Groups["cod"].Value.Trim() : null),
                Desc = (m.Groups["d"].Success ? m.Groups["d"].Value.Trim() : null),
                Notas = (m.Groups["n"].Success ? m.Groups["n"].Value.Trim() : null),
            };

            if (m.Groups["q"].Success && int.TryParse(m.Groups["q"].Value, out var q))
                ti.Q = q;

            if (m.Groups["pu"].Success &&
                decimal.TryParse(m.Groups["pu"].Value,
                                 System.Globalization.NumberStyles.Any,
                                 System.Globalization.CultureInfo.InvariantCulture,
                                 out var pu))
                ti.Pu = pu;

            // ✅ Normalizaciones:
            // - Los HEAD que vienen como MENU se tratan como HEAD
            // - Si quieres unificar CHICHA a CHI (opcional), deja la línea siguiente
            if (ti.Tipo == "MENU") ti.Tipo = "HEAD";
            if (ti.Tipo == "CHICHA") ti.Tipo = "CHI";

            // Limpia el tag del texto de notas que quedará visible
            obsLimpia = obsLimpia.Remove(m.Index, m.Length).Trim();

            return ti;
        }
        private void PrecargarPedidoAbiertoDeMesa()
        {
            try
            {
                int mesaNum = (SesionActual.Mesa != null) ? SesionActual.Mesa.Numero : 0;
                if (mesaNum == 0) { _esReingresoMesa = false; return; }

                string mesaStr = mesaNum.ToString("000");

                var dao = new DAOPedido();
                string numPed = dao.ObtenerNumPedAbiertoPorMesa(mesaStr);
                if (string.IsNullOrWhiteSpace(numPed))
                {
                    _numPedActual = null;
                    _hayPedidoAbierto = false;
                    _esReingresoMesa = false;   // primer ingreso
                    return;
                }

                _numPedActual = numPed;
                _hayPedidoAbierto = true;
                _esReingresoMesa = true;        // re-ingreso

                var cab = dao.ObtenerCabeceraPorNum(numPed);
                if (cab != null && cab.NUM_PERS.HasValue && cab.NUM_PERS.Value > 0)
                    txtNPersonas.Text = cab.NUM_PERS.Value.ToString();

                var detalles = dao.ObtenerDetallePorPedido(numPed);
                if (detalles == null || detalles.Count == 0)
                    return;

                flpLineas.SuspendLayout();
                flpLineas.Controls.Clear();

                var grupos = new Dictionary<string, Grupo>(StringComparer.OrdinalIgnoreCase);
                var planas = new List<ceDPedido>();

                // === Particionar por grupo (CDG_COMB o fallback/tag) ===
                foreach (var d in detalles)
                {
                    // 1) Determinar ID de grupo
                    string grp = "";
                    try
                    {
                        var pComb = d.GetType().GetProperty("CDG_COMB");
                        if (pComb != null)
                        {
                            object v = pComb.GetValue(d, null);
                            string s = Convert.ToString(v, CultureInfo.InvariantCulture);
                            s = (s ?? "").Trim();
                            if (!string.IsNullOrWhiteSpace(s))
                            {
                                grp = s.TrimStart('0');
                                if (grp.Length == 0) grp = "0";
                            }
                        }
                    }
                    catch { /* ignorar */ }

                    // Fallback por CDG_FPRD si no hay grupo
                    if (string.IsNullOrWhiteSpace(grp))
                    {
                        try
                        {
                            var prop = d.GetType().GetProperty("CDG_FPRD");
                            if (prop != null)
                            {
                                object val = prop.GetValue(d, null);
                                if (val != null)
                                {
                                    int n;
                                    if (val is int) n = (int)val;
                                    else if (!int.TryParse(Convert.ToString(val, CultureInfo.InvariantCulture), out n)) n = 0;
                                    if (n != 0) grp = n.ToString(CultureInfo.InvariantCulture);
                                }
                            }
                        }
                        catch { grp = ""; }
                    }

                    // 2) Parsear tag y limpiar OBS_PPRD visible
                    string limpio;
                    var ti = TryParseTag(d.OBS_PPRD, out limpio);
                    d.OBS_PPRD = limpio;

                    // Si el tag tiene C=... úsalo como grupo
                    if (string.IsNullOrWhiteSpace(grp) && ti != null && !string.IsNullOrWhiteSpace(ti.Id))
                        grp = ti.Id.Trim();

                    // 3) Si sigue sin grupo → va a planas
                    if (string.IsNullOrWhiteSpace(grp))
                    {
                        planas.Add(d);
                        continue;
                    }

                    // Normalizar tipo del tag
                    string tipo = (ti?.Tipo ?? "").Trim().ToUpperInvariant();
                    if (tipo == "MENU") tipo = "HEAD";
                    if (tipo == "CHICHA") tipo = "CHI";

                    // 4) Agregar al grupo correspondiente
                    if (!grupos.TryGetValue(grp, out var g))
                    {
                        g = new Grupo();
                        grupos[grp] = g;
                    }

                    if (!string.IsNullOrEmpty(tipo))
                    {
                        switch (tipo)
                        {
                            case "HEAD": g.Head = d; g.HeadTag = ti; break;
                            case "JUG": g.Jugos.Add(d); g.JugosTag.Add(ti); break;
                            case "BEB": g.Bebidas.Add(d); g.BebidasTag.Add(ti); break;
                            case "TAM": g.Tamales.Add(d); g.TamalesTag.Add(ti); break;
                            case "CHI": g.Chicha = d; g.ChichaTag = ti; break;
                            default:
                                // Heurística: si tiene precio, podría ser la cabecera más cara
                                if (d.PRE_IGV > 0m)
                                {
                                    if (g.Head == null || d.PRE_IGV >= g.Head.PRE_IGV)
                                    {
                                        if (g.Head != null) { g.Jugos.Add(g.Head); g.JugosTag.Add(null); }
                                        g.Head = d;
                                    }
                                    else
                                    {
                                        g.Jugos.Add(d); g.JugosTag.Add(null);
                                    }
                                }
                                else
                                {
                                    g.Jugos.Add(d); g.JugosTag.Add(null);
                                }
                                break;
                        }
                    }
                    else
                    {
                        // Sin tipo de tag: misma heurística
                        if (d.PRE_IGV > 0m)
                        {
                            if (g.Head == null || d.PRE_IGV >= g.Head.PRE_IGV)
                            {
                                if (g.Head != null) { g.Jugos.Add(g.Head); g.JugosTag.Add(null); }
                                g.Head = d;
                            }
                            else
                            {
                                g.Jugos.Add(d); g.JugosTag.Add(null);
                            }
                        }
                        else
                        {
                            g.Jugos.Add(d); g.JugosTag.Add(null);
                        }
                    }
                }

                // === Renderizar grupos ===
                foreach (var kv in grupos)
                {
                    var g = kv.Value;

                    // Si no hay head, flatear
                    if (g.Head == null)
                    {
                        if (g.Chicha != null) planas.Add(g.Chicha);
                        planas.AddRange(g.Jugos);
                        planas.AddRange(g.Bebidas);
                        planas.AddRange(g.Tamales);
                        continue;
                    }

                    // Resolver datos del HEAD
                    string codHead =
                        (!string.IsNullOrWhiteSpace(g.HeadTag?.Cod)) ? g.HeadTag.Cod.Trim().PadLeft(10, '0') :
                        (!string.IsNullOrWhiteSpace(g.Head.COD10)) ? g.Head.COD10.Trim().PadLeft(10, '0') :
                        (g.Head.CDG_PROD > 0 ? g.Head.CDG_PROD.ToString().PadLeft(10, '0') : "");

                    string desHead =
                        !string.IsNullOrWhiteSpace(g.HeadTag?.Desc)
                            ? g.HeadTag.Desc
                            : ResolverDescripcionProducto(codHead, g.Head.CDG_PROD);

                    int cantidadHead =
                        g.HeadTag?.Q.HasValue == true
                            ? Math.Max(1, g.HeadTag.Q.Value)
                            : Math.Max(1, (int)Math.Round(g.Head.CAN_PPRD, 0, MidpointRounding.AwayFromZero));

                    decimal puHeadConIgv =
                        g.HeadTag?.Pu.HasValue == true ? g.HeadTag.Pu.Value : g.Head.PRE_IGV;

                    bool esMenu = (g.Chicha != null || g.ChichaTag != null);

                    if (esMenu)
                    {
                        // ====== MENÚ ======
                        var itemM = new MenuPedidoItem();
                        itemM.SetMenu(codHead, desHead, cantidadHead, puHeadConIgv);

                        string notasHead = !string.IsNullOrWhiteSpace(g.HeadTag?.Notas) ? g.HeadTag.Notas
                                          : (g.Head?.OBS_PPRD ?? string.Empty);
                        if (!string.IsNullOrWhiteSpace(notasHead))
                            itemM.SetNotas(MenuPedidoItem.ZonaNotas.Menu, notasHead);

                        if (g.ChichaTag != null || g.Chicha != null)
                        {
                            string codCh =
                                (!string.IsNullOrWhiteSpace(g.ChichaTag?.Cod)) ? g.ChichaTag.Cod.Trim().PadLeft(10, '0') :
                                (!string.IsNullOrWhiteSpace(g.Chicha?.COD10)) ? g.Chicha.COD10.Trim().PadLeft(10, '0') :
                                (g.Chicha != null && g.Chicha.CDG_PROD > 0 ? g.Chicha.CDG_PROD.ToString().PadLeft(10, '0') : "");

                            string desCh =
                                !string.IsNullOrWhiteSpace(g.ChichaTag?.Desc)
                                    ? g.ChichaTag.Desc
                                    : ResolverDescripcionProducto(codCh, g.Chicha?.CDG_PROD ?? 0);

                            string notasCh = !string.IsNullOrWhiteSpace(g.ChichaTag?.Notas) ? g.ChichaTag.Notas
                                           : (g.Chicha?.OBS_PPRD ?? string.Empty);

                            itemM.SetChicha(codCh, desCh, notasCh, cantidadHead);
                        }

                        // Referencia extendida (para borrado exacto)
                        string combHead = null, numItemHead = null;
                        try { var p = g.Head.GetType().GetProperty("CDG_COMB"); if (p != null) combHead = Convert.ToString(p.GetValue(g.Head, null)) ?? ""; } catch { }
                        try { var p = g.Head.GetType().GetProperty("NUM_ITEM"); if (p != null) numItemHead = Convert.ToString(p.GetValue(g.Head, null)) ?? ""; } catch { }

                        itemM.SetRefDetalle(new CapaEntidad.DetalleRef
                        {
                            NumPed = _numPedActual,
                            CdgFprd = g.Head.CDG_FPRD,
                            CdgComb = (combHead ?? "").Trim(),
                            NumItem = (numItemHead ?? "").Trim()
                        });

                        AjustarAnchoItem(itemM);
                        flpLineas.Controls.Add(itemM);
                        MarcarSoloLectura(itemM);  // 🔒 bloquea edición/eliminación
                    }
                    else
                    {
                        // ====== COMBO ======
                        var itemC = new ComboPedidoItem
                        {
                            AgruparJugosIguales = true,
                            AgruparBebidasIguales = true,
                            AgruparTamalesIguales = true
                        };

                        // Jugos
                        for (int i = 0; i < g.Jugos.Count; i++)
                        {
                            var d = g.Jugos[i];
                            var t = (i < g.JugosTag.Count) ? g.JugosTag[i] : null;

                            string cod =
                                (!string.IsNullOrWhiteSpace(t?.Cod)) ? t.Cod.Trim().PadLeft(10, '0') :
                                (!string.IsNullOrWhiteSpace(d.COD10)) ? d.COD10.Trim().PadLeft(10, '0') :
                                (d.CDG_PROD > 0 ? d.CDG_PROD.ToString().PadLeft(10, '0') : "");

                            string des =
                                !string.IsNullOrWhiteSpace(t?.Desc) ? t.Desc : ResolverDescripcionProducto(cod, d.CDG_PROD);

                            string notas = !string.IsNullOrWhiteSpace(t?.Notas) ? t.Notas : (d.OBS_PPRD ?? string.Empty);

                            int veces = Math.Max(1, (int)Math.Round(d.CAN_PPRD, 0, MidpointRounding.AwayFromZero));
                            for (int k = 0; k < veces; k++)
                                itemC.AddJugoUnidad(cod, des, 0m, notas, null);
                        }

                        // Bebidas
                        for (int i = 0; i < g.Bebidas.Count; i++)
                        {
                            var d = g.Bebidas[i];
                            var t = (i < g.BebidasTag.Count) ? g.BebidasTag[i] : null;

                            string cod =
                                (!string.IsNullOrWhiteSpace(t?.Cod)) ? t.Cod.Trim().PadLeft(10, '0') :
                                (!string.IsNullOrWhiteSpace(d.COD10)) ? d.COD10.Trim().PadLeft(10, '0') :
                                (d.CDG_PROD > 0 ? d.CDG_PROD.ToString().PadLeft(10, '0') : "");

                            string des =
                                !string.IsNullOrWhiteSpace(t?.Desc) ? t.Desc : ResolverDescripcionProducto(cod, d.CDG_PROD);

                            string notas = !string.IsNullOrWhiteSpace(t?.Notas) ? t.Notas : (d.OBS_PPRD ?? string.Empty);

                            int veces = Math.Max(1, (int)Math.Round(d.CAN_PPRD, 0, MidpointRounding.AwayFromZero));
                            for (int k = 0; k < veces; k++)
                                itemC.AddBebidaUnidad(cod, des, 0m, notas, false);
                        }

                        // Tamales
                        for (int i = 0; i < g.Tamales.Count; i++)
                        {
                            var d = g.Tamales[i];
                            var t = (i < g.TamalesTag.Count) ? g.TamalesTag[i] : null;

                            string cod =
                                (!string.IsNullOrWhiteSpace(t?.Cod)) ? t.Cod.Trim().PadLeft(10, '0') :
                                (!string.IsNullOrWhiteSpace(d.COD10)) ? d.COD10.Trim().PadLeft(10, '0') :
                                (d.CDG_PROD > 0 ? d.CDG_PROD.ToString().PadLeft(10, '0') : "");

                            string des =
                                !string.IsNullOrWhiteSpace(t?.Desc) ? t.Desc : ResolverDescripcionProducto(cod, d.CDG_PROD);

                            string notas = !string.IsNullOrWhiteSpace(t?.Notas) ? t.Notas : (d.OBS_PPRD ?? string.Empty);

                            int veces = Math.Max(1, (int)Math.Round(d.CAN_PPRD, 0, MidpointRounding.AwayFromZero));
                            for (int k = 0; k < veces; k++)
                                itemC.AddTamalUnidad(cod, des, 0m, notas, null);
                        }

                        // Encabezado (PU CON IGV)
                        itemC.SetCombo(codHead, desHead, cantidadHead, puHeadConIgv);

                        // Referencia extendida (para borrado fino)
                        string combHead = null, numItemHead = null;
                        try { var p = g.Head.GetType().GetProperty("CDG_COMB"); if (p != null) combHead = Convert.ToString(p.GetValue(g.Head, null)) ?? ""; } catch { }
                        try { var p = g.Head.GetType().GetProperty("NUM_ITEM"); if (p != null) numItemHead = Convert.ToString(p.GetValue(g.Head, null)) ?? ""; } catch { }

                        itemC.SetRefDetalle(new CapaEntidad.DetalleRef
                        {
                            NumPed = _numPedActual,
                            CdgFprd = g.Head.CDG_FPRD,
                            CdgComb = (combHead ?? "").Trim(),
                            NumItem = (numItemHead ?? "").Trim()
                        });

                        AjustarAnchoItem(itemC);
                        flpLineas.Controls.Add(itemC);
                        MarcarSoloLectura(itemC);    // 🔒 bloquea edición/eliminación
                    }
                }

                // === Renderizar líneas planas ===
                foreach (var d in planas)
                {
                    string cod10 =
                        !string.IsNullOrWhiteSpace(d.COD10) ? d.COD10.Trim().PadLeft(10, '0') :
                        (d.CDG_PROD > 0 ? d.CDG_PROD.ToString().PadLeft(10, '0') : "");

                    string descripcion =
                        !string.IsNullOrWhiteSpace(d.DESCRIPCION)
                            ? d.DESCRIPCION
                            : ResolverDescripcionProducto(cod10, d.CDG_PROD);

                    decimal puConIgv = d.PRE_IGV; // unitario CON IGV
                    int cantidad = Math.Max(1, (int)Math.Round(d.CAN_PPRD, 0, MidpointRounding.AwayFromZero));
                    string notas = d.OBS_PPRD ?? string.Empty;

                    // Intentar leer COMB y NUM_ITEM del renglón plano (si existen) para el borrado fino
                    string comb = null, numItem = null;
                    try { var p = d.GetType().GetProperty("CDG_COMB"); if (p != null) comb = Convert.ToString(p.GetValue(d, null)) ?? ""; } catch { }
                    try { var p = d.GetType().GetProperty("NUM_ITEM"); if (p != null) numItem = Convert.ToString(p.GetValue(d, null)) ?? ""; } catch { }

                    var li = new CapaPresentacion.Controles.LineaPedidoItem();
                    li.Configurar(cod10, descripcion, cantidad, puConIgv, notas);

                    // 🔸 Guarda referencia extendida para poder borrar exacto por NUM_ITEM
                    li.SetRefDetalle(new CapaEntidad.DetalleRef
                    {
                        NumPed = _numPedActual,
                        CdgFprd = d.CDG_FPRD,
                        NumItem = TryGet<string>(d, "NUM_ITEM", ""), // ← debe existir en ceDPedido
                        CdgComb = TryGet<string>(d, "CDG_COMB", "")
                    });

                    AjustarAnchoItem(li);
                    flpLineas.Controls.Add(li);
                    MarcarSoloLectura(li); // 🔒 bloquea edición/eliminación
                }

                flpLineas.ResumeLayout(true);

                ActualizarSubtotal();
                LineaSelection.Clear();
                btnEliminar.Enabled = false;
                btnComentarioLbr.Enabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "No se pudo precargar el pedido abierto de la mesa.\n\nDetalle: " + ex.Message,
                    "Pedidos",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
            var numPed8 = _numPedActual; // o la variable que uses
            if (!string.IsNullOrWhiteSpace(numPed8))
            {
                var fecPedLocal = ObtenerFecPedLocal(numPed8);
                StartTimerMesaActual(fecPedLocal);           // <-- ENCIENDE el timer si aún no estaba
            }
        }



        private bool _esReingresoMesa = false;

        private bool PermiteEliminarAhora()
        {
            // Si NO es reingreso de mesa, se permite eliminar sin pedir supervisor
            if (!_esReingresoMesa) return true;

            // En reingreso: solicitar código de supervisor (CDG_USR) y validar
            using (var dlg = new frmCodAdmin())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;

                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;

                var usr = (dlg.CodigoIngresado ?? "").Trim();  // CDG_USR
                if (string.IsNullOrWhiteSpace(usr))
                    return false;

                bool ok = _cnVendedor.EsAdminPorUsuario(usr);  // valida por USUARIO (no por legajo)
                if (!ok)
                    MessageBox.Show("Código de supervisor inválido (CDG_USR).",
                                    "Eliminar", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                return ok;
            }
        }


        //private void HabilitarAccionesDeLinea(Control ctrlLinea)
        //{
        //    //// Ejemplos; ajusta a tus controles reales:
        //    //if (ctrlLinea is MenuPedidoItem m)
        //    //{
        //    //    m.OnEliminarSolicitado -= Control_OnEliminarSolicitado;
        //    //    m.OnEliminarSolicitado += Control_OnEliminarSolicitado;
        //    //}
        //    //else if (ctrlLinea is ComboPedidoItem c)
        //    //{
        //    //    c.OnEliminarSolicitado -= Control_OnEliminarSolicitado;
        //    //    c.OnEliminarSolicitado += Control_OnEliminarSolicitado;
        //    //}
        //    //else if (ctrlLinea is LineaPedidoItem l)
        //    //{
        //    //    l.OnEliminarSolicitado -= Control_OnEliminarSolicitado;
        //    //    l.OnEliminarSolicitado += Control_OnEliminarSolicitado;
        //    //}

        //    // si tenías readonly para textos/cantidad, puedes dejarlo;
        //    // lo único crítico es NO bloquear el botón/el evento eliminar.
        //}

        private void Control_OnEliminarSolicitado(object sender, ceDPedido linea)
        {
            if (!PermiteEliminarAhora()) return;
            EjecutarEliminacion(linea);
        }
        private void EjecutarEliminacion(ceDPedido _)
        {
            EjecutarEliminacionSeleccionActual();
        }
        private void EjecutarEliminacionSeleccionActual()
        {
            //ActualizarSubtotal();

            var sel = LineaSelection.Actual;
            if (sel == null) return;

            var view = sel.View;
            // Bloqueadas: solo si estás reingresando y pasa validación
            //if (_lineasBloqueadas.Contains(view))
            //{
            //    if (!_esReingresoMesa || !PermiteEliminarAhora()) return;
            //}

            var svc = new CapaNegocio.cnPedido();

            // === LineaPedidoItem ⇒ borrar por NUM_ITEM ===
            if (sel is LineaPedidoItem li)
            {
                var refd = li.GetRefDetalle();
                if (refd != null && !string.IsNullOrWhiteSpace(refd.NumPed) && !string.IsNullOrWhiteSpace(refd.NumItem))
                {
                    // elimina SOLO esa fila
                    svc.EliminarDetallePorNumItem(refd.NumPed, refd.NumItem);
                }
                else if (refd != null && !string.IsNullOrWhiteSpace(refd.NumPed) && refd.CdgFprd > 0)
                {
                    // fallback (histórico) si te quedaras sin NumItem
                    svc.EliminarDetalle(refd.NumPed, refd.CdgFprd);
                    svc.RecalcularTotales(refd.NumPed);
                }

                QuitarControlSeleccionado(view);
                return;
            }

            // === ComboPedidoItem / MenuPedidoItem ⇒ preferir CDG_COMB; si no, NUM_ITEM ===
            if (sel is ComboPedidoItem ci)
            {
                var refd = ci.GetRefDetalle();
                if (refd != null && !string.IsNullOrWhiteSpace(refd.NumPed))
                {
                    if (!string.IsNullOrWhiteSpace(refd.CdgComb))
                        svc.EliminarDetallePorCombo(refd.NumPed, refd.CdgComb);
                    else if (!string.IsNullOrWhiteSpace(refd.NumItem))
                        svc.EliminarDetallePorNumItem(refd.NumPed, refd.NumItem);
                }

                QuitarControlSeleccionado(view);
                return;
            }

            if (sel is MenuPedidoItem mi)
            {
                var refd = mi.GetRefDetalle();
                if (refd != null && !string.IsNullOrWhiteSpace(refd.NumPed))
                {
                    if (!string.IsNullOrWhiteSpace(refd.CdgComb))
                        svc.EliminarDetallePorCombo(refd.NumPed, refd.CdgComb);
                    else if (!string.IsNullOrWhiteSpace(refd.NumItem))
                        svc.EliminarDetallePorNumItem(refd.NumPed, refd.NumItem);
                }

                QuitarControlSeleccionado(view);
                return;
            }
        }
        private void QuitarControlSeleccionado(Control view)
        {
            var parent = view?.Parent as Control;
            if (parent == null) return;

            int idx = parent.Controls.IndexOf(view);
            _lineasBloqueadas.Remove(view);
            parent.Controls.Remove(view);
            view.Dispose();

            LineaSelection.Clear();
            var vecino = BuscarSeleccionableVecino(parent, idx);
            if (vecino != null) LineaSelection.Select(vecino, true);
            btnEliminar.Enabled = (LineaSelection.Actual != null) && !_lineasBloqueadas.Contains(LineaSelection.Actual.View);
            btnComentarioLbr.Enabled = btnEliminar.Enabled;
            ActualizarSubtotal();
        }

        // Agrupación por correlación C= (para reconstruir cada combo/menú)
        private sealed class Grupo
        {
            public ceDPedido Head; public TagInfo HeadTag;
            public List<ceDPedido> Jugos = new List<ceDPedido>();
            public List<TagInfo> JugosTag = new List<TagInfo>();
            public List<ceDPedido> Bebidas = new List<ceDPedido>();
            public List<TagInfo> BebidasTag = new List<TagInfo>();
            public List<ceDPedido> Tamales = new List<ceDPedido>();
            public List<TagInfo> TamalesTag = new List<TagInfo>();
            public ceDPedido Chicha; public TagInfo ChichaTag;
        }

        private string ResolverDescripcionProducto(string cod10, int cdgProdInt)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(cod10))
                {
                    // 1) caché
                    ceProductos pCache;
                    if (_cachePorCodigo != null &&
                        _cachePorCodigo.TryGetValue(cod10, out pCache) &&
                        pCache != null &&
                        !string.IsNullOrWhiteSpace(pCache.Descripcion))
                    {
                        return pCache.Descripcion;
                    }

                    // 2) servicio
                    var p = _svcProductos != null ? _svcProductos.Obtener(cod10, "001") : null;
                    if (p != null)
                    {
                        if (_cachePorCodigo != null) _cachePorCodigo[cod10] = p;
                        if (!string.IsNullOrWhiteSpace(p.Descripcion))
                            return p.Descripcion;
                    }
                }

                // 3) fallback a DAOProductos (por CDG_PROD int)
                if (cdgProdInt > 0)
                {
                    var nom = DAOProductos.ObtenerDescripcion(cdgProdInt);
                    if (!string.IsNullOrWhiteSpace(nom))
                        return nom.Trim();
                }
            }
            catch
            {
                // ignora y cae al fallback
            }

            // 4) último fallback a código
            return string.IsNullOrWhiteSpace(cod10) ? cdgProdInt.ToString() : cod10;
        }
        // Líneas que vienen de BD y no deben editarse/eliminarse
        private readonly HashSet<Control> _lineasBloqueadas = new HashSet<Control>();

        private void MarcarSoloLectura(Control item)
        {
            if (item == null) return;
            _lineasBloqueadas.Add(item);
            item.Tag = "RO";                 // marca simple por si te sirve en el futuro
            item.Cursor = Cursors.Arrow;     // evita “manito” si la tuviera
                                             // Opcional: tenue visual (si te gusta)
                                             // item.Enabled = true;  // IMPORTANT: mantener habilitado para que se vea “normal”
        }
        private bool PermiteEliminarAhoraPara(Control view)
        {
            // Si no estamos en reingreso, no pide admin
            if (!_esReingresoMesa) return true;

            // Si la línea no es “antigua” (no viene de BD), no pide admin
            var refAnt = (view as dynamic)?.RefDetalle as CapaEntidad.DetalleRef;
            if (refAnt == null) return true;

            using (var dlg = new CapaPresentacion.Administrador.frmCodAdmin())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                if (dlg.ShowDialog(this) != DialogResult.OK) return false;

                var usr = dlg.CodigoIngresado;
                if (string.IsNullOrWhiteSpace(usr)) return false;

                // ✅ VALIDACIÓN por USUARIO
                return _cnVendedor.EsAdminPorUsuario(usr);
            }
        }

        ///////-----TIMER MESAS---------/////
        /// <summary> Devuelve el formulario de salón si está abierto. </summary>
        private frmSPrincipal GetSalonForm()
        {
            return Application.OpenForms.OfType<frmSPrincipal>().FirstOrDefault();
        }

        /// <summary> Inicia el timer de la mesa actual con la FEC_PED indicada (horario local). </summary>
        private void StartTimerMesaActual(DateTime fecPedLocal)
        {
            var salon = GetSalonForm();
            if (salon == null) return;

            var mesa = (SesionActual.Mesa != null) ? SesionActual.Mesa.Numero : 0;
            if (mesa <= 0) return;

            salon.StartMesa(mesa, fecPedLocal);
        }

        /// <summary> Detiene/oculta el timer de la mesa actual. </summary>
        private void StopTimerMesaActual()
        {
            var salon = GetSalonForm();
            if (salon == null) return;

            var mesa = (SesionActual.Mesa != null) ? SesionActual.Mesa.Numero : 0;
            if (mesa <= 0) return;

            salon.StopMesa(mesa);
        }

        /// <summary> Lee FEC_PED real desde BD; si no existe, usa DateTime.Now. </summary>
        private DateTime ObtenerFecPedLocal(string numPed8)
        {
            try
            {
                var cab = _cnPed.ObtenerCabeceraPorNum(numPed8);
                if (cab != null && cab.FEC_PED != default(DateTime))
                    return cab.FEC_PED;
            }
            catch { /* ignora y usa Now */ }
            return DateTime.Now;
        }

        //----------------------PRECUENTA-----------------------------//
        private void btnPrecuenta_Click(object sender, EventArgs e)
        {
            var numPed = _numPedActual;
            if (string.IsNullOrWhiteSpace(numPed))
            {
                MessageBox.Show("No hay un pedido guardado para precuenta.", "Aviso",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var frm = new frmPreCuenta(numPed, usarPrecioConIgv: false, incluirIGV0: false))
                frm.ShowDialog(this);
        }
    }
}