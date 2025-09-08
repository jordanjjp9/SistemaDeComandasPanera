using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CapaEntidad;
using CapaNegocio;
using CapaPresentacion.Controles;
using CapaPresentacion.Helpers;
using CapaPresentacion.Notas;

namespace CapaPresentacion
{
    public partial class frmMenuPrincipal : Form
    {

        private cnProducto _svcProductos;

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
        private const string COD_DESAYUNO_CONTINENTAL = "0000000457";

        private const string COD_DESAYUNO_CRIOLLO = "0000000458";
        private const string COD_DESAYUNO_PANERA = "0000000461";

        private const string CHICHA_ALMUERZO_CODE = "0000000868";

        private int? _cantidadForzada;  // prioridad sobre txtCantidad cuando no es null

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

            this.Shown += frmMenuPrincipal_Shown;
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

            ////// 🔸 Habilitar/Deshabilitar botones según la SELECCIÓN GLOBAL
            ////LineaSelection.Changed += (s, ev) =>
            ////{
            ////    var sel = LineaSelection.Actual;              // puede ser LineaPedidoItem o ComboPedidoItem
            ////    bool haySel = (sel != null);

            ////    btnEliminar.Enabled = haySel;

            ////    // Comentario libre: habilitar para líneas normales y combos
            ////    btnComentarioLbr.Enabled = (sel is LineaPedidoItem) || (sel is ComboPedidoItem);
            ////};
            LineaSelection.Changed += (s, ev) =>
            {
                var sel = LineaSelection.Actual;              // puede ser LineaPedidoItem, ComboPedidoItem o MenuPedidoItem
                bool haySel = (sel != null);

                btnEliminar.Enabled = haySel;

                // ⬇️ incluir MenuPedidoItem
                btnComentarioLbr.Enabled =
                    (sel is LineaPedidoItem) ||
                    (sel is ComboPedidoItem) ||
                    (sel is MenuPedidoItem);
            };

            // Estado inicial de botones (nada seleccionado)
            btnEliminar.Enabled = false;
            btnComentarioLbr.Enabled = false;

            // Recalcular total al agregar/quitar controles
            flpLineas.ControlAdded += (_, __) => ActualizarSubtotal();
            flpLineas.ControlRemoved += (_, __) => ActualizarSubtotal();

            MostrarEnCentral(new CapaPresentacion.Botoneras.frmPastas());
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
                //// cantidad*código
                //var qStr = partes[0].Trim();
                //var codStr = partes[1].Trim();

                //if (!int.TryParse(qStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) || q <= 0)
                //    return (false, 1, null);

                //if (string.IsNullOrEmpty(codStr))
                //    return (true, q, null); // aún no teclea el código, entrada parcial válida

                //if (CodigoSoloNumerico && !codStr.All(char.IsDigit))
                //    return (false, q, null);

                //return (true, q, codStr);

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
            //var sel = LineaSelection.Actual;
            //if (sel == null)
            //{
            //    MessageBox.Show("Selecciona primero un ítem.", "Comentario",
            //                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //if (sel is LineaPedidoItem lp)
            //{
            //    using (var dlg = new frmComentarioLbr())
            //    {
            //        dlg.Texto = lp.Notas ?? string.Empty;   // o lp.GetNotasRaw() si lo tienes
            //        dlg.TextoInicial = dlg.Texto;
            //        if (dlg.ShowDialog(this) == DialogResult.OK)
            //            lp.SetNotas(dlg.Comentario);
            //    }
            //}
            //else if (sel is ComboPedidoItem ci)
            //{
            //    if (!ci.EditarUltimoJugoOBebida(this))
            //        MessageBox.Show("No hay jugo/bebida para editar notas.", "Comentario",
            //                        MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}
            //else if (sel is MenuPedidoItem mi)
            //{
            //    // Edita la zona que tocaste por última vez: Menu o Chicha
            //    var zona = mi.ZonaActiva;
            //    if (zona == MenuPedidoItem.ZonaNotas.Ninguna)
            //        zona = MenuPedidoItem.ZonaNotas.Chicha; // por si acaso

            //    using (var dlg = new frmComentarioLbr())
            //    {
            //        dlg.Texto = mi.GetNotasRaw(zona);
            //        dlg.TextoInicial = dlg.Texto;

            //        // Título informativo (opcional)
            //        dlg.Text = (zona == MenuPedidoItem.ZonaNotas.Menu)
            //                   ? "Comentario del Menú"
            //                   : "Comentario de la Chicha";

            //        if (dlg.ShowDialog(this) == DialogResult.OK)
            //            mi.SetNotas(zona, dlg.Comentario);   // reemplaza notas de esa zona
            //    }
            //}

            var sel = LineaSelection.Actual;
            if (sel == null)
            {
                MessageBox.Show("Selecciona primero un ítem.", "Comentario",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            //else if (sel is ComboPedidoItem ci)
            //{
            //    // SHIFT + click: editar SIEMPRE el encabezado (txtCombo)
            //    if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            //    {
            //        using (var dlg = new frmComentarioLbr())
            //        {
            //            dlg.Text = "Comentario del Combo";
            //            dlg.Texto = ci.GetNotasEncabezadoRaw();
            //            dlg.TextoInicial = dlg.Texto;
            //            if (dlg.ShowDialog(this) == DialogResult.OK)
            //                ci.SetNotasEncabezado(dlg.Comentario);
            //        }
            //        return;
            //    }

            //    // Comportamiento habitual: editar última bebida/jugo/tamal;
            //    // si no hay subitems, cae a editar encabezado.
            //    if (!ci.EditarUltimoJugoOBebida(this))
            //    {
            //        using (var dlg = new frmComentarioLbr())
            //        {
            //            dlg.Text = "Comentario del Combo";
            //            dlg.Texto = ci.GetNotasEncabezadoRaw();
            //            dlg.TextoInicial = dlg.Texto;
            //            if (dlg.ShowDialog(this) == DialogResult.OK)
            //                ci.SetNotasEncabezado(dlg.Comentario);
            //        }
            //    }
            //}
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
                if (ci.TieneSubItemEditable())   // <— método que te pasé antes
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
            //if (e.KeyCode != Keys.Enter) return;

            //var parsed = TryParseCantidadCodigo(txtCantidad.Text);

            //if (!parsed.ok)
            //{
            //    MessageBox.Show("Formato inválido. Usa: cantidad o cantidad*código");
            //    e.SuppressKeyPress = true;
            //    return;
            //}

            //// Si solo hay cantidad, no buscamos aún (dejas listo para escribir *código)
            //if (parsed.codigo == null)
            //{
            //    e.SuppressKeyPress = true;
            //    return;
            //}

            //// Normaliza el código: si es numérico, pad a 10
            //string codIngresado = parsed.codigo.Trim();
            //string cod10 = codIngresado.All(char.IsDigit) ? codIngresado.PadLeft(LARGO_CODIGO, '0') : codIngresado;

            //// Buscar: exacto -> termina en
            //var producto = BuscarProductoPorCodigoExacto(cod10); 
            //if (producto == null && codIngresado.All(char.IsDigit)) producto = BuscarProductoPorCodigoTerminaEn(codIngresado);

            //if (producto == null)
            //{
            //    MessageBox.Show($"No se encontró el producto '{codIngresado}'.");
            //    e.SuppressKeyPress = true;
            //    return;
            //}


            //// Mostrar usando la cantidad parseada (evita inconsistencias)
            ////  AgregarLinea(producto, parsed.cantidad);
            //SeleccionarProducto(producto, parsed.cantidad);
            //SeleccionarPorCodigoConCantidad(producto.Codigo, parsed.cantidad);

            //// Limpia y listo para el siguiente input
            //txtCantidad.Clear();
            //txtCantidad.Focus();

            //e.SuppressKeyPress = true;

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

        private void btnListarProductos_Click(object sender, EventArgs e)
        {
            using (var lstprd = new frmListaProductos())
            {
                lstprd.StartPosition = FormStartPosition.CenterParent;
                var r = lstprd.ShowDialog(this);
                if (r == DialogResult.OK && !string.IsNullOrWhiteSpace(lstprd.SelectedCodigo))
                {
                    // Reutiliza tu flujo actual: respeta txtCantidad y muestra en txtProducto
                    Hijo_ProductoSeleccionado(lstprd.SelectedCodigo);
                }
            }
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
            //flpLineas.RemoveSelected();

            //// Habilita/deshabilita el botón según quede selección
            //btnEliminar.Enabled = (flpLineas.GetSeleccion() != null);

            ////var sel = LineaSelection.Actual;
            ////if (sel == null) return;

            ////var ctrl = sel.View;            // raíz del control seleccionado

            ////var parent = ctrl.Parent;
            ////if (parent != null)
            ////{
            ////    parent.Controls.Remove(ctrl);
            ////    ctrl.Dispose();
            ////}

            ////LineaSelection.Clear();
            ////btnEliminar.Enabled = false;
            ////btnComentarioLbr.Enabled = false;

            ////ActualizarSubtotal();

            var sel = LineaSelection.Actual;
            if (sel == null) return;

            var view = sel.View;                   // Control raíz del ítem seleccionado
            var parent = view?.Parent as Control;
            if (parent == null) return;

            // índice del control que se va
            int index = parent.Controls.IndexOf(view);

            parent.Controls.Remove(view);
            view.Dispose();

            // limpiar selección global
            LineaSelection.Clear();

            // intentar seleccionar un vecino (mismo índice o anterior)
            var vecino = BuscarSeleccionableVecino(parent, index);
            if (vecino != null)
            {
                LineaSelection.Select(vecino, true);   // esto dispara LineaSelection.Changed y habilita botones
            }
            else
            {
                // no quedó nada seleccionable
                btnEliminar.Enabled = false;
                btnComentarioLbr.Enabled = false;
            }

            ActualizarSubtotal();
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

        private void frmMenuPrincipal_Shown(object sender, EventArgs e)
        {
            if (_pidioNumeroPersonas) return;
            _pidioNumeroPersonas = true;

            using (var dlg = new frmNumeroPersonas())
            {
                var r = dlg.ShowDialog(this);
                if (r != DialogResult.OK)
                {
                    // Si cancelan, cierra el principal (o decide qué comportamiento quieres)
                    Close();
                    return;
                }

                // Copia la cantidad al textbox del principal
                txtNPersonas.Text = dlg.Cantidad.ToString();
            }
        }

        // Para transportar lo que el usuario escogió en cada paso del wizard
        private sealed class SeleccionSimple
        {
            public string Codigo { get; set; }
            public string Descripcion { get; set; }
            public decimal PrecioExtra { get; set; } // 0 si no aplica
        }




        private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        {
            if (prod == null || cantidad <= 0) return;

            // ===== 0) Paso previo SOLO para DESAYUNO CONTINENTAL =====
            string notasEncabezado = string.Empty;
            string cod10 = (prod.Codigo ?? "").Trim().PadLeft(10, '0');
            if (cod10 == COD_DESAYUNO_CONTINENTAL)
            {
                using (var pre = new CapaPresentacion.Notas.frmNDesayunoContinental
                {
                    ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}"
                })
                {
                    if (pre.ShowDialog(this) != DialogResult.OK) return;
                    notasEncabezado = pre.Notas ?? string.Empty;
                }
            }

            // 1) Crear item combo
            var item = new CapaPresentacion.Controles.ComboPedidoItem
            {
                AgruparJugosIguales = true,
                AgruparBebidasIguales = true
            };

            // 2) Elegir jugo + NBebidas por unidad (y contar “grande”)
            int calientesPendientes = cantidad;
            for (int i = 1; i <= cantidad; i++)
            {
                // 2.1 Jugo
                CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple jugoSel = null;
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

                // 2.2 Notas bebidas (NBebidas) – muestra arriba el jugo elegido
                string notasJugo = string.Empty;
                using (var frmN = new CapaPresentacion.Notas.frmNBebidas
                {
                    ProductoBaseTexto = jugoSel.Descripcion
                })
                {
                    if (frmN.ShowDialog(this) == DialogResult.OK)
                    {
                        notasJugo = frmN.Notas ?? string.Empty;
                        if (frmN.CuposCalienteConsumidos > 0 && calientesPendientes > 0)
                            calientesPendientes -= 1;
                    }
                }

                // 2.3 Agregar jugo a la UI (respeta agrupación)
                item.AddJugoUnidad(jugoSel.Descripcion, jugoSel.PrecioExtra, notasJugo, null);
            }

            // 3) (Opcional) Bebidas calientes por bloque si todavía faltan
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
                            item.AddBebidaUnidad(b.Descripcion, b.PrecioExtra, string.Empty, false);
                    }
                }
            }

            // 4) Precio final = base + promedio de extras
            decimal puBase = PrecioDe(prod);
            decimal puFinal = puBase + item.GetExtraPromedioTotalPorUnidad(cantidad);

            // 5) Encabezado + NOTAS del paso previo (si hubo)
            item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puFinal);
            if (!string.IsNullOrWhiteSpace(notasEncabezado))
                item.AppendNotasEncabezado(notasEncabezado);

            // 6) Mostrar en el panel izquierdo
            flpLineas.SuspendLayout();
            flpLineas.Controls.Add(item);
            flpLineas.ResumeLayout();

            LineaSelection.Select(item, true);   // <<< selección única
            btnEliminar.Enabled = (LineaSelection.Actual != null);
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

            // ===== 1) J U G O (1 por desayuno) + notas de jugo (NBebidas) =====
            int calientesPendientes = cantidad; // 1 caliente por desayuno

            for (int i = 1; i <= cantidad; i++)
            {
                // Elegir jugo de ESTE desayuno
                List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> sel = null;
                using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
                {
                    frmJ.CantidadRequerida = 1;              // <-- SIEMPRE 1 por desayuno
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
                            calientesPendientes -= 1;          // “GRANDE” descuenta 1 caliente
                    }
                }

                item.AddJugoUnidad(jugo.Descripcion, jugo.PrecioExtra, notasJugo, /*forzarIndividual*/ null);
            }

            // ===== 2) B E B I D A S  C A L I E N T E S  (1 por desayuno, menos “GRANDE”) =====
            if (calientesPendientes > 0)
            {
                using (var frmB = new CapaPresentacion.Notas.frmCBebidasCalientes())
                {
                    frmB.CantidadRequerida = calientesPendientes;    // <-- NO depende de los tamales
                    frmB.ListaPrecio = "001";
                    frmB.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}";

                    if (frmB.ShowDialog(this) == DialogResult.OK)
                    {
                        var seleB = frmB.Selecciones ?? new List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple>();
                        foreach (var b in seleB)
                            item.AddBebidaUnidad(b.Descripcion, b.PrecioExtra, string.Empty, /*forzarIndividual*/ false);
                    }
                }
            }

            // ===== 3) T A M A L E S  (ÚNICO lugar donde se multiplica) =====
            int totalTamales = tamalesPorUnidad * cantidad;           // <-- SOLO TAMAL multiplica
            if (totalTamales > 0)
            {
                using (var frmT = new CapaPresentacion.Notas.frmCDesayunoTamal())
                {
                    frmT.CantidadRequerida = totalTamales;            // p.ej., Panera: 2 * cantidad
                    frmT.ListaPrecio = "001";
                    frmT.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}";

                    if (frmT.ShowDialog(this) == DialogResult.OK)
                    {
                        var sels = frmT.Selecciones ?? new List<CapaPresentacion.Notas.frmCDesayunoTamal.SeleccionSimple>();
                        foreach (var t in sels)
                            item.AddTamalUnidad(t.Descripcion, t.PrecioExtra, /*notas*/ string.Empty, /*forzarIndividual*/ null);
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // ===== 4) Precio por unidad (extras promediados: jugo + bebida; los tamales usualmente 0) =====
            decimal puBase = PrecioDe(prod);
            decimal puFinal = puBase + item.GetExtraPromedioTotalPorUnidad(cantidad);

            // ===== 5) Pintar combo =====
            item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puFinal);

            flpLineas.SuspendLayout();
            flpLineas.Controls.Add(item);
            flpLineas.ResumeLayout();

            // Seleccionar y habilitar botones conforme a la selección global
            LineaSelection.Select(item, true);
            btnEliminar.Enabled = (LineaSelection.Actual != null);
            btnComentarioLbr.Enabled = (LineaSelection.Actual != null);

            ActualizarSubtotal();
        }



        // =============================================================
        // =================== Flujo MenuPedidoItem ====================
        // =============================================================

        private void AgregarMenuPasta(ceProductos menuProd, int cantidad)
        {


            // 1) Resolver chicha
            var chicha = _svcProductos.Obtener(CHICHA_ALMUERZO_CODE, "001")
                         ?? new ceProductos { Codigo = CHICHA_ALMUERZO_CODE, Descripcion = "CHICHA ALMUERZO", PrecioUnitario = 0m };

            // 2) Notas de NBebidas
            string notas = string.Empty;
            using (var frm = new frmNBebidas())
            {
                frm.ProductoBaseTexto = $"{cantidad} x {chicha.Descripcion}";
                frm.TextoInicial = string.Empty;

                if (frm.ShowDialog(this) != DialogResult.OK) return;
                notas = frm.Notas ?? string.Empty;
            }

            // 3) Crear y poblar el control
            var item = new MenuPedidoItem();

            // 👉 AQUÍ está la diferencia: usa la API del control para que pinte el precio
            decimal puMenu = PrecioDe(menuProd);                 // PRE_SOL (o VAL_SOL de respaldo)
            item.SetMenu(menuProd.Codigo, menuProd.Descripcion, cantidad, puMenu);
            item.SetChicha(chicha.Descripcion, notas, cantidad); // cantidad y notas normalizadas

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

        //private static void TrySetText(Control c, string text)
        //{
        //    if (c == null) return;
        //    try
        //    {
        //        var p = c.GetType().GetProperty("Text");
        //        p?.SetValue(c, text ?? string.Empty, null);
        //    }
        //    catch { /* ignore */ }
        //}
        //private static string NormalizarNotas(string raw)
        //{
        //    if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        //    var lines = (raw ?? string.Empty)
        //                .Replace("\r\n", "\n")
        //                .Replace("\r", "\n")
        //                .Split('\n')
        //                .Select(l => (l ?? string.Empty).Trim())
        //                .Where(l => l.Length > 0)
        //                .Select(l => l.StartsWith("-") ? l : "- " + l);

        //    return string.Join(Environment.NewLine, lines);
        //}

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
    }
}
