using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaDatos;
using CapaEntidad;
using CapaPresentacion.Helpers;
using CapaNegocio;
using System.Text.RegularExpressions;
using System.Globalization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using CapaPresentacion.Botoneras;
using CapaPresentacion.Controles;
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

            // Desengancha al anterior si lo había
            if (_formHijoActual is ISelectorProducto selOld)
                selOld.ProductoSeleccionado -= Hijo_ProductoSeleccionado;

            _formHijoActual = formHijo;

            formHijo.TopLevel = false;
            formHijo.FormBorderStyle = FormBorderStyle.None;
            formHijo.Dock = DockStyle.Fill;

            panelHost.Controls.Add(formHijo);
            formHijo.Show();
            formHijo.BringToFront();

            // 👇 Engancha si implementa ISelectorProducto
            if (formHijo is ISelectorProducto sel)
                sel.ProductoSeleccionado += Hijo_ProductoSeleccionado;
        }

      //  private DragScroller _lineasScroller;

        private void frmMenuPrincipal_Load(object sender, EventArgs e)
        {
            //// Cabecera
            //txtAmb.Text = SesionActual.Ambiente ?? "";
            //txtMesa.Text = SesionActual.Mesa?.Numero.ToString() ?? "";
            //txtVendedor.Text = SesionActual.Vendedor?.Nombre ?? "";

            //// Solo lectura visual
            //txtAmb.ReadOnly = txtMesa.ReadOnly = txtVendedor.ReadOnly = true;

            //// Servicios / cache
            //_svcProductos = new cnProducto();
            //RecargarCache("001");

            //// Cantidad por defecto y validaciones
            //txtCantidad.KeyPress += txtCantidad_KeyPress;
            //if (string.IsNullOrWhiteSpace(txtCantidad.Text)) txtCantidad.Text = "1";

            //// Botonera superior
            //var sec = new frmBotoneraPrincipal(this);
            //MostrarFormularioEnPanel(sec, pnlCSup);

            //// ===== Lista de líneas (panel izquierdo) =====
            //flpLineas.FlowDirection = FlowDirection.TopDown;  // vertical
            //flpLineas.WrapContents = false;
            //flpLineas.AutoScroll = true;

            //// Arrastre + inercia + oculta barras (lo hace el scroller internamente)
            //_lineasScroller = new CapaPresentacion.Helpers.DragScroller(flpLineas, CapaPresentacion.Helpers.DragAxis.Vertical);

            //// Habilitar/Deshabilitar botones según selección de una línea
            //LineaPedidoItem.SeleccionCambio += (_, __) =>
            //{
            //    bool haySel = (LineaPedidoItem.SeleccionActual != null);
            //    btnEliminar.Enabled = haySel;
            //    btnComentarioLbr.Enabled = haySel;  // si quieres que comentario libre solo funcione con un item seleccionado
            //};
            //// estado inicial
            //bool haySelIni = (LineaPedidoItem.SeleccionActual != null);
            //btnEliminar.Enabled = haySelIni;
            //btnComentarioLbr.Enabled = haySelIni;

            //flpLineas.ControlAdded += (_, __) => ActualizarSubtotal();
            //flpLineas.ControlRemoved += (_, __) => ActualizarSubtotal();

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

            // 🔸 Habilitar/Deshabilitar botones según la SELECCIÓN GLOBAL
            LineaSelection.Changed += (s, ev) =>
            {
                var sel = LineaSelection.Actual;              // puede ser LineaPedidoItem o ComboPedidoItem
                bool haySel = (sel != null);

                btnEliminar.Enabled = haySel;

                // Comentario libre: habilitar para líneas normales y combos
                btnComentarioLbr.Enabled = (sel is LineaPedidoItem) || (sel is ComboPedidoItem);
            };

            // Estado inicial de botones (nada seleccionado)
            btnEliminar.Enabled = false;
            btnComentarioLbr.Enabled = false;

            // Recalcular total al agregar/quitar controles
            flpLineas.ControlAdded += (_, __) => ActualizarSubtotal();
            flpLineas.ControlRemoved += (_, __) => ActualizarSubtotal();
        }

        private int CantidadActual()
        {
            var p = TryParseCantidadCodigo(txtCantidad.Text);
            return (p.ok && p.cantidad > 0) ? p.cantidad : 1;
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

            ceProductos prod;
            if (!_cachePorCodigo.TryGetValue(cod10, out prod) || prod == null)
            {
                prod = _svcProductos.Obtener(cod10, "001");
                if (prod != null) _cachePorCodigo[cod10] = prod;
            }
            if (prod == null)
            {
                MessageBox.Show($"Producto {cod10} no encontrado.", "Productos",
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int cantidad = CantidadActual();
            if (cantidad <= 0) cantidad = 1;

            // 👉 delega todo aquí (combos, helados, normal)
            SeleccionarProducto(prod, cantidad);
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
                // cantidad*código
                var qStr = partes[0].Trim();
                var codStr = partes[1].Trim();

                if (!int.TryParse(qStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var q) || q <= 0)
                    return (false, 1, null);

                if (string.IsNullOrEmpty(codStr))
                    return (true, q, null); // aún no teclea el código, entrada parcial válida

                if (CodigoSoloNumerico && !codStr.All(char.IsDigit))
                    return (false, q, null);

                return (true, q, codStr);
            }

            return (false, 1, null);
        }



        private void btnComentarioLbr_Click(object sender, EventArgs e)
        {
            //var sel = flpLineas.GetSeleccion(); // tu helper para obtener el LineaPedidoItem seleccionado
            //if (sel == null)
            //{
            //    MessageBox.Show("Selecciona primero un producto de la lista.", "Notas",
            //                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //using (var dlg = new frmComentarioLbr())
            //{
            //    dlg.StartPosition = FormStartPosition.CenterParent;
            //    dlg.TextoInicial = sel.Notas;       // precarga
            //    if (dlg.ShowDialog(this) == DialogResult.OK)
            //        sel.Notas = dlg.Comentario;     // devuelve al panel (con saltos preservados)
            //}

            //var lp = CapaPresentacion.Controles.LineaPedidoItem.SeleccionActual;
            //if (lp == null)
            //{
            //    MessageBox.Show("Selecciona primero una línea.", "Comentario libre",
            //                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    return;
            //}

            //using (var dlg = new frmComentarioLbr())
            //{
            //    // Precargar SOLO las notas (sin encabezado)
            //    dlg.Texto = lp.GetNotasRaw();
            //    dlg.TextoInicial = dlg.Texto;

            //    if (dlg.ShowDialog(this) == DialogResult.OK)
            //    {
            //        lp.SetNotas(dlg.Comentario);   // reemplaza las notas y repinta
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
                    dlg.Texto = lp.GetNotasRaw();
                    dlg.TextoInicial = dlg.Texto;

                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        lp.SetNotas(dlg.Comentario);
                }
            }
            else if (sel is ComboPedidoItem ci)
            {
                if (!ci.EditarUltimoJugoOBebida(this))
                    MessageBox.Show("No hay jugo/bebida para editar notas.", "Comentario",
                                    MessageBoxButtons.OK, MessageBoxIcon.Information);
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

            // Si solo hay cantidad, no buscamos aún (dejas listo para escribir *código)
            if (parsed.codigo == null)
            {
                e.SuppressKeyPress = true;
                return;
            }

            // Normaliza el código: si es numérico, pad a 10
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

            // Mostrar usando la cantidad parseada (evita inconsistencias)
            //  AgregarLinea(producto, parsed.cantidad);
            SeleccionarProducto(producto, parsed.cantidad);

            // Limpia y listo para el siguiente input
            txtCantidad.Clear();
            txtCantidad.Focus();

            e.SuppressKeyPress = true;
        }



        private void SeleccionarProducto(ceProductos prod, int cantidad)
        {
            if (prod == null || cantidad <= 0) return;

            // 1) ¿Es un combo de desayuno? -> abre el wizard (jugos + notas) y termina aquí
            if (_svcProductos.EsComboDesayuno(prod))
            {
                EjecutarWizardDesayunoPorUnidad(prod, cantidad);   // ← usa este
                return;
            }

            // 2) Flujo existente (ej. helados con notas especiales)
            string cod10 = (prod.Codigo ?? "").Trim().PadLeft(LARGO_CODIGO, '0');

            if (RequiereNotas(cod10))
            {
                using (var dlg = new CapaPresentacion.Notas.frmNHelados())
                {
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.Cantidad = cantidad;
                    dlg.Producto = prod.Descripcion ?? prod.Codigo;

                    if (dlg.ShowDialog(this) == DialogResult.OK)
                        //AgregarLineaPedido(prod, cantidad, dlg.Notas);
                        AgregarProducto(prod.Codigo, prod.Descripcion, cantidad, PrecioDe(prod));
                }
            }
            else
            {
                // 3) Producto normal sin notas -> agrega línea directa
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
            //if (prod == null || cantidad <= 0) return;

            //decimal pu = PrecioDe(prod); // tu helper existente

            //var item = new LineaPedidoItem();
            //item.Configurar(prod.Codigo, prod.Descripcion, cantidad, pu, notas ?? string.Empty);

            //flpLineas.SuspendLayout();
            //flpLineas.Controls.Add(item);
            //flpLineas.ResumeLayout();

            //// Selecciona la línea recién agregada y hace scroll hacia ella
            //LineaPedidoItem.Seleccionar(item, true);

            //// (opcional) habilita/deshabilita eliminar según haya selección
            //btnEliminar.Enabled = (LineaPedidoItem.SeleccionActual != null);

            //// ... tu código existente ...
            //LineaPedidoItem.Seleccionar(item, true);
            //btnEliminar.Enabled = (LineaPedidoItem.SeleccionActual != null);

            //ActualizarSubtotal();   // ⬅️ aquí

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

            var sel = LineaSelection.Actual;
            if (sel == null) return;

            var ctrl = sel.View;            // raíz del control seleccionado

            var parent = ctrl.Parent;
            if (parent != null)
            {
                parent.Controls.Remove(ctrl);
                ctrl.Dispose();
            }

            LineaSelection.Clear();
            btnEliminar.Enabled = false;
            btnComentarioLbr.Enabled = false;

            ActualizarSubtotal();
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

        // Orquesta NBebidas -> (opcional) CBebidasCalientes según N o N-1.
        // El callback "appendNotas" te deja anexar las notas a la línea que acabas de crear.
        private void EjecutarFlujoBebidasParaDesayuno(
            int cantidadDesayunos,
            string descripcionDesayuno,
            string listaPrecio,
            Action<string> appendNotas)
        {
            int calientesPendientes = cantidadDesayunos; // N

            // 1) Primero NBebidas (si tocan "GRANDE", consume 1 caliente)
            using (var nb = new frmNBebidas
            {
                ProductoBaseTexto = $"{cantidadDesayunos} x {descripcionDesayuno}",
                TextoInicial = ""
            })
            {
                if (nb.ShowDialog(this) == DialogResult.OK)
                {
                    calientesPendientes -= Math.Min(1, nb.CuposCalienteConsumidos);
                    var notasFria = (nb.Notas ?? "").Trim();
                    if (notasFria.Length > 0) appendNotas?.Invoke(notasFria);
                }
            }

            // 2) Si aún faltan calientes, abre el de calientes por "calientesPendientes"
            if (calientesPendientes > 0)
            {
                using (var bc = new frmCBebidasCalientes
                {
                    ListaPrecio = listaPrecio,
                    Titulo = "Bebidas Calientes",
                    ProductoBaseTexto = $"{cantidadDesayunos} x {descripcionDesayuno}",
                    CantidadRequerida = calientesPendientes,
                    ReglaAdicionalLecheActiva = true
                })
                {
                    if (bc.ShowDialog(this) == DialogResult.OK)
                    {
                        var notasCaliente = BuildNotasDesdeSelecciones(bc.Selecciones);
                        if (notasCaliente.Length > 0) appendNotas?.Invoke(notasCaliente);
                    }
                }
            }
        }

        // Formatea selecciones como:
        // - 2 x DESCAFEINADO = S/ 2.00
        // - AMERICANO
        private string BuildNotasDesdeSelecciones(
            System.Collections.Generic.List<frmCBebidasCalientes.SeleccionSimple> sels)
        {
            if (sels == null || sels.Count == 0) return string.Empty;

            var grupos = sels
                .GroupBy(s => new { s.Codigo, Descripcion = (s.Descripcion ?? "").Trim().ToUpperInvariant(), s.PrecioExtra })
                .Select(g => new
                {
                    Cant = g.Count(),
                    g.Key.Descripcion,
                    g.Key.PrecioExtra,
                    Total = g.Count() * g.Key.PrecioExtra
                })
                .ToList();

            var sb = new System.Text.StringBuilder();
            foreach (var x in grupos)
            {
                if (sb.Length > 0) sb.AppendLine();
                if (x.Cant <= 1)
                {
                    if (x.PrecioExtra > 0m)
                        sb.Append("- ").Append(x.Descripcion).Append(" = S/ ").Append(x.Total.ToString("0.00"));
                    else
                        sb.Append("- ").Append(x.Descripcion);
                }
                else
                {
                    if (x.PrecioExtra > 0m)
                        sb.Append("- ").Append(x.Cant).Append(" x ").Append(x.Descripcion).Append(" = S/ ").Append(x.Total.ToString("0.00"));
                    else
                        sb.Append("- ").Append(x.Cant).Append(" x ").Append(x.Descripcion);
                }
            }
            return sb.ToString();
        }
        private void AgregarProducto(string cod, string desc, int cantidad, decimal precioUnit)
        {
            var item = flpLineas.AddLinea(cod, desc, cantidad, precioUnit, notas: "");

            bool esDesayuno = desc.IndexOf("DESAYUNO", StringComparison.OrdinalIgnoreCase) >= 0;
            if (esDesayuno)
            {
                EjecutarFlujoBebidasParaDesayuno(cantidad, desc, "001", notas =>
                {
                    item.AppendNotas(notas);
                });
            }

            // RecalcularSubtotal();
        }


        //}
        //private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        //{
        //    if (prod == null || cantidad <= 0) return;

        //    // 0) Crear item visual y configurar preferencias de agrupado
        //    var item = new CapaPresentacion.Controles.ComboPedidoItem
        //    {
        //        AgruparJugosIguales = true,   // <<— clave para que salga: 2 x JUGO DE PIÑA + notas
        //        AgruparBebidasIguales = true
        //    };

        //    // 1) Por cada unidad: un jugo + (opcional) notas libres para ese jugo
        //    for (int i = 1; i <= cantidad; i++)
        //    {
        //        List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> sel = null;

        //        using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
        //        {
        //            frmJ.CantidadRequerida = 1;
        //            frmJ.ListaPrecio = "001";
        //            frmJ.ProductoBaseTexto = $"1 x {prod.Descripcion}  ({i}/{cantidad})";

        //            if (frmJ.ShowDialog(this) != DialogResult.OK) return;

        //            sel = frmJ.Selecciones ?? new List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
        //            if (sel.Count == 0) return;
        //        }

        //        var jugo = sel[0];

        //        string notasJugo = string.Empty;
        //        using (var frmN = new CapaPresentacion.Notas.frmNBebidas())
        //        {
        //            // si quieres precargar algo en el cuadro, ponlo aquí:
        //            // frmN.TextoInicial = "";
        //            if (frmN.ShowDialog(this) == DialogResult.OK)
        //                notasJugo = frmN.Notas ?? string.Empty;
        //        }

        //        // NO forzar individual: así respeta AgruparJugosIguales = true
        //        item.AddJugoUnidad(jugo.Descripcion, jugo.PrecioExtra, notasJugo, /*forzarIndividual*/ null);
        //    }

        //    // 2) Elegir Bebidas calientes para el total (bloque)
        //    using (var frmB = new CapaPresentacion.Notas.frmCBebidasCalientes())
        //    {
        //        frmB.CantidadRequerida = cantidad;
        //        frmB.ListaPrecio = "001";
        //        frmB.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}";

        //        if (frmB.ShowDialog(this) == DialogResult.OK)
        //        {
        //            // Asegura tipos compatibles (por si la clase de SeleccionSimple difiere)
        //            var seleB = frmB.Selecciones ?? new List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple>();
        //            foreach (var b in seleB)
        //            {
        //                item.AddBebidaUnidad(b.Descripcion, b.PrecioExtra, /*notas*/ string.Empty, /*forzarIndividual*/ false);
        //            }
        //        }
        //        else
        //        {
        //            // Si las bebidas son obligatorias, puedes abortar aquí.
        //            // return;
        //        }
        //    }

        //    // 3) Precio final por unidad = base + promedio de extras (jugos + bebidas)
        //    decimal puBase = PrecioDe(prod);
        //    decimal puFinal = puBase + item.GetExtraPromedioTotalPorUnidad(cantidad);

        //    // 4) Encabezado del combo y pintar en el panel izquierdo
        //    item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puFinal);

        //    flpLineas.SuspendLayout();
        //    flpLineas.Controls.Add(item);
        //    flpLineas.ResumeLayout();

        //    CapaPresentacion.Controles.ComboPedidoItem.Seleccionar(item, true);
        //    btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);
        //}
        //private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        //{
        //    if (prod == null || cantidad <= 0) return;

        //    var item = new CapaPresentacion.Controles.ComboPedidoItem
        //    {
        //        AgruparJugosIguales = true,
        //        AgruparBebidasIguales = true
        //    };

        //    // 🔸 al inicio: tantos calientes como desayunos
        //    int calientesPendientes = cantidad;

        //    // 1) Por cada unidad: elegir jugo y (opcional) notas; si tocan GRANDE, consume 1 caliente
        //    for (int i = 1; i <= cantidad; i++)
        //    {
        //        List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> sel = null;

        //        using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
        //        {
        //            frmJ.CantidadRequerida = 1;
        //            frmJ.ListaPrecio = "001";
        //            frmJ.ProductoBaseTexto = $"1 x {prod.Descripcion}  ({i}/{cantidad})";

        //            if (frmJ.ShowDialog(this) != DialogResult.OK) return;

        //            sel = frmJ.Selecciones ?? new List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
        //            if (sel.Count == 0) return;
        //        }

        //        var jugo = sel[0];

        //        string notasJugo = string.Empty;
        //        using (var frmN = new CapaPresentacion.Notas.frmNBebidas())
        //        {
        //            // (opcional) precarga: frmN.TextoInicial = "";
        //            if (frmN.ShowDialog(this) == DialogResult.OK)
        //            {
        //                notasJugo = frmN.Notas ?? string.Empty;

        //                // 🔸 si tocaron GRANDE en este NBebidas, consume 1 caliente
        //                if (frmN.CuposCalienteConsumidos > 0 && calientesPendientes > 0)
        //                    calientesPendientes -= 1;
        //            }
        //        }

        //        // Respetar agrupación de jugos (no forzar individual)
        //        item.AddJugoUnidad(jugo.Descripcion, jugo.PrecioExtra, notasJugo, null);
        //    }

        //    // 2) Elegir bebidas calientes SOLO si aún faltan
        //    if (calientesPendientes > 0)
        //    {
        //        using (var frmB = new CapaPresentacion.Notas.frmCBebidasCalientes())
        //        {
        //            frmB.CantidadRequerida = calientesPendientes;   // 🔸 antes usabas 'cantidad'
        //            frmB.ListaPrecio = "001";
        //            frmB.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}";

        //            if (frmB.ShowDialog(this) == DialogResult.OK)
        //            {
        //                var seleB = frmB.Selecciones ?? new List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple>();
        //                foreach (var b in seleB)
        //                    item.AddBebidaUnidad(b.Descripcion, b.PrecioExtra, string.Empty, false);
        //            }
        //        }
        //    }

        //    // 3) PU final = base + promedio de extras
        //    decimal puBase = PrecioDe(prod);
        //    decimal puFinal = puBase + item.GetExtraPromedioTotalPorUnidad(cantidad);

        //    // 4) Pintar combo en panel izquierdo
        //    item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puFinal);

        //    flpLineas.SuspendLayout();
        //    flpLineas.Controls.Add(item);
        //    flpLineas.ResumeLayout();

        //    CapaPresentacion.Controles.ComboPedidoItem.Seleccionar(item, true);
        //    btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);
        //}
        private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        {
            if (prod == null || cantidad <= 0) return;

            var item = new CapaPresentacion.Controles.ComboPedidoItem
            {
                AgruparJugosIguales = true,
                AgruparBebidasIguales = true
            };

            // Al inicio: tantas calientes como desayunos
            int calientesPendientes = cantidad;

            // 1) Por cada unidad: elegir jugo y (opcional) notas; si tocan GRANDE, consume 1 caliente
            for (int i = 1; i <= cantidad; i++)
            {
                List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> sel = null;

                using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
                {
                    frmJ.CantidadRequerida = 1;
                    frmJ.ListaPrecio = "001";
                    frmJ.ProductoBaseTexto = $"1 x {prod.Descripcion}  ({i}/{cantidad})";

                    if (frmJ.ShowDialog(this) != DialogResult.OK) return;

                    sel = frmJ.Selecciones ?? new List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
                    if (sel.Count == 0) return;
                }

                var jugo = sel[0]; // la única selección de jugo para esta unidad

                string notasJugo = string.Empty;
                using (var frmN = new CapaPresentacion.Notas.frmNBebidas()
                {
                    // >>> Mostrar el jugo elegido en el encabezado del NBebidas
                    ProductoBaseTexto = $"1 x {jugo.Descripcion} ({i}/{cantidad})",
                    // TextoInicial = ""   // si quisieras precargar algo
                })
                {
                    if (frmN.ShowDialog(this) == DialogResult.OK)
                    {
                        notasJugo = frmN.Notas ?? string.Empty;

                        // Si tocaron "GRANDE" en este NBebidas, consume 1 caliente (máx. 1)
                        if (frmN.CuposCalienteConsumidos > 0 && calientesPendientes > 0)
                            calientesPendientes -= 1;
                    }
                }

                // Respetar la agrupación de jugos (no forzar individual)
                item.AddJugoUnidad(jugo.Descripcion, jugo.PrecioExtra, notasJugo, /*forzarIndividual*/ null);
            }

            // 2) Elegir bebidas calientes SOLO si aún faltan
            if (calientesPendientes > 0)
            {
                using (var frmB = new CapaPresentacion.Notas.frmCBebidasCalientes())
                {
                    frmB.CantidadRequerida = calientesPendientes;   // antes: cantidad
                    frmB.ListaPrecio = "001";
                    frmB.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}";

                    if (frmB.ShowDialog(this) == DialogResult.OK)
                    {
                        var seleB = frmB.Selecciones ?? new List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple>();
                        foreach (var b in seleB)
                            item.AddBebidaUnidad(b.Descripcion, b.PrecioExtra, /*notas*/ string.Empty, /*forzarIndividual*/ false);
                    }
                }
            }

            // 3) PU final = base + promedio de extras (jugos + bebidas)
            decimal puBase = PrecioDe(prod);
            decimal puFinal = puBase + item.GetExtraPromedioTotalPorUnidad(cantidad);

            // 4) Pintar combo en el panel izquierdo
            item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puFinal);

            flpLineas.SuspendLayout();
            flpLineas.Controls.Add(item);
            flpLineas.ResumeLayout();

            //CapaPresentacion.Controles.ComboPedidoItem.Seleccionar(item, true);
            //btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);
            LineaSelection.Select(item, true);   // <<< selección única
            btnEliminar.Enabled = (LineaSelection.Actual != null);
        }


        private void ActualizarSubtotal()
        {
            decimal total = 0m;

            foreach (Control c in flpLineas.Controls)
            {
                if (c is CapaPresentacion.Controles.LineaPedidoItem li)
                    total += li.Importe;                 // Cantidad * PU

                else if (c is CapaPresentacion.Controles.ComboPedidoItem ci)
                    total += ci.Total;                   // Cantidad * PU (con extras promediados)
            }

            txtSubtotal.Text = $"S/ {total:0.00}";
        }


    }
}
