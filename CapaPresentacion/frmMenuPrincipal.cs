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
            _lineasScroller = new CapaPresentacion.Helpers.DragScroller(flpLineas, CapaPresentacion.Helpers.DragAxis.Vertical);

            // Habilitar/Deshabilitar botones según selección de una línea
            LineaPedidoItem.SeleccionCambio += (_, __) =>
            {
                bool haySel = (LineaPedidoItem.SeleccionActual != null);
                btnEliminar.Enabled = haySel;
                btnComentarioLbr.Enabled = haySel;  // si quieres que comentario libre solo funcione con un item seleccionado
            };
            // estado inicial
            bool haySelIni = (LineaPedidoItem.SeleccionActual != null);
            btnEliminar.Enabled = haySelIni;
            btnComentarioLbr.Enabled = haySelIni;


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
            var sel = flpLineas.GetSeleccion(); // tu helper para obtener el LineaPedidoItem seleccionado
            if (sel == null)
            {
                MessageBox.Show("Selecciona primero un producto de la lista.", "Notas",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var dlg = new frmComentarioLbr())
            {
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.TextoInicial = sel.Notas;       // precarga
                if (dlg.ShowDialog(this) == DialogResult.OK)
                    sel.Notas = dlg.Comentario;     // devuelve al panel (con saltos preservados)
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
                        AgregarLineaPedido(prod, cantidad, dlg.Notas);
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


            if (prod == null || cantidad <= 0) return;

            decimal pu = PrecioDe(prod); // tu helper existente

            var item = new LineaPedidoItem();
            item.Configurar(prod.Codigo, prod.Descripcion, cantidad, pu, notas ?? string.Empty);

            flpLineas.SuspendLayout();
            flpLineas.Controls.Add(item);
            flpLineas.ResumeLayout();

            // Selecciona la línea recién agregada y hace scroll hacia ella
            LineaPedidoItem.Seleccionar(item, true);

            // (opcional) habilita/deshabilita eliminar según haya selección
            btnEliminar.Enabled = (LineaPedidoItem.SeleccionActual != null);
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            flpLineas.RemoveSelected();

            // Habilita/deshabilita el botón según quede selección
            btnEliminar.Enabled = (flpLineas.GetSeleccion() != null);
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

        //////private void EjecutarWizardDesayunoSoloJugos(ceProductos prod, int cantidad)
        //////{
        //////    if (prod == null || cantidad <= 0) return;

        //////    // ===== 1) Elegir jugos (obligatorio: 'cantidad' selecciones) =====
        //////    System.Collections.Generic.List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> seleccionesJugos;

        //////    using (var frm = new CapaPresentacion.Notas.frmCJugoDesayuno())
        //////    {
        //////        frm.CantidadRequerida = cantidad;
        //////        // Lo que se ve arriba en el diálogo (ej. "2 x DESAYUNO AMERICANO")
        //////        frm.ProductoBaseTexto = string.Format("{0} x {1}", cantidad, (prod.Descripcion ?? prod.Codigo));
        //////        // Lista de precios a usar para leer los extras de BD
        //////        frm.ListaPrecio = "001";

        //////        var r = frm.ShowDialog(this);
        //////        if (r != DialogResult.OK) return;

        //////        seleccionesJugos = frm.Selecciones ?? new System.Collections.Generic.List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
        //////        if (seleccionesJugos.Count == 0) return; // nada elegido → aborta
        //////    }

        //////    // ===== 2) Armar texto de JUGOS + calcular recargos =====
        //////    var sbJugos = new System.Text.StringBuilder();
        //////    decimal extraTotal = 0m;

        //////    for (int i = 0; i < seleccionesJugos.Count; i++)
        //////    {
        //////        var s = seleccionesJugos[i];
        //////        if (i > 0) sbJugos.AppendLine();

        //////        sbJugos.Append("- ").Append(s.Descripcion);
        //////        if (s.PrecioExtra > 0m)
        //////            sbJugos.Append(" = S/ ").Append(s.PrecioExtra.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));

        //////        extraTotal += s.PrecioExtra;
        //////    }

        //////    string notasJugos = sbJugos.ToString();

        //////    // ===== 3) Notas libres de bebidas (opcionales) =====
        //////    // Precargamos con lo ya escrito de jugos para que el usuario siga añadiendo debajo.
        //////    string notasFinalJugo = notasJugos; // por defecto, solo jugos
        //////    using (var frmB = new CapaPresentacion.Notas.frmNBebidas())
        //////    {
        //////        frmB.TextoInicial = notasJugos; // se verá en su textbox; los botones agregan líneas
        //////        var r = frmB.ShowDialog(this);
        //////        if (r == DialogResult.OK)
        //////        {
        //////            // El diálogo devuelve TODO el contenido (lo precargado + lo que el usuario añadió)
        //////            if (!string.IsNullOrWhiteSpace(frmB.Notas))
        //////                notasFinalJugo = frmB.Notas.TrimEnd();
        //////        }
        //////    }

        //////    // ===== 4) Precio final por unidad (base + prorrateo de extras) =====
        //////    decimal extraPorUnidad = (cantidad > 0) ? (extraTotal / cantidad) : 0m;
        //////    decimal puConExtra = PrecioDe(prod) + extraPorUnidad;

        //////    // ===== 5) Crear y mostrar el ComboPedidoItem =====
        //////    var item = new CapaPresentacion.Controles.ComboPedidoItem();
        //////    item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puConExtra);
        //////    item.SetJugoDesayuno(notasFinalJugo); // aquí va JUGO + (opcional) notas de bebidas

        //////    flpLineas.SuspendLayout();
        //////    flpLineas.Controls.Add(item);
        //////    flpLineas.ResumeLayout();

        //////    CapaPresentacion.Controles.ComboPedidoItem.Seleccionar(item, true);
        //////    btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);
        //////}

        ////private string ConstruirResumenJugo(
        ////    List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> sel, int cantidad)
        ////{
        ////    if (sel == null || sel.Count == 0) return string.Empty;

        ////    bool allSame = sel.All(s =>
        ////        string.Equals(s.Descripcion, sel[0].Descripcion, StringComparison.OrdinalIgnoreCase) &&
        ////        s.PrecioExtra == sel[0].PrecioExtra);

        ////    var s0 = sel[0];
        ////    if (allSame)
        ////    {
        ////        string baseText = (cantidad > 1) ? $"{cantidad} x {s0.Descripcion}" : s0.Descripcion;
        ////        if (s0.PrecioExtra > 0m)
        ////            baseText += $" = S/ {s0.PrecioExtra:0.00}";
        ////        return baseText;
        ////    }
        ////    else
        ////    {
        ////        // Mezcla de jugos: usa el primero como resumen (evita saturar el panel)
        ////        string baseText = s0.Descripcion;
        ////        if (s0.PrecioExtra > 0m)
        ////            baseText += $" = S/ {s0.PrecioExtra:0.00}";
        ////        return baseText;
        ////    }
        ////}

        // De lo que devolvió frmNBebidas (nb.Notas) elimina la primera línea si coincide con 'primeraLinea'.
        // Estandariza como líneas "- texto" para integrarlas bajo el jugo.
        //////////private static string ExtraerSoloExtras(string notas, string primeraLinea)
        //////////{
        //////////    if (string.IsNullOrEmpty(notas)) return string.Empty;

        //////////    string norm = notas.Replace("\r\n", "\n");
        //////////    string pl = (primeraLinea ?? string.Empty).Trim();

        //////////    var lines = norm.Split('\n');
        //////////    var sb = new StringBuilder();

        //////////    for (int i = 0; i < lines.Length; i++)
        //////////    {
        //////////        string line = (lines[i] ?? string.Empty).Trim();
        //////////        if (i == 0 && !string.IsNullOrWhiteSpace(pl) &&
        //////////            string.Equals(line, pl, StringComparison.OrdinalIgnoreCase))
        //////////        {
        //////////            // omite la primera línea (el resumen duplicado)
        //////////            continue;
        //////////        }
        //////////        if (line.Length == 0) continue;

        //////////        if (sb.Length > 0) sb.AppendLine();
        //////////        sb.Append(line.StartsWith("- ") ? line : "- " + line);
        //////////    }
        //////////    return sb.ToString();
        //////////}

        //private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        //{
        //    if (prod == null || cantidad <= 0) return;

        //    // 1) Crea el panel del combo (todavía sin PU final).
        //    var item = new CapaPresentacion.Controles.ComboPedidoItem();

        //    // Acumulador de extras para calcular PU final
        //    decimal extraTotal = 0m;

        //    // 2) Recolecta: (CJugo → NBebidas) por cada unidad
        //    for (int i = 1; i <= cantidad; i++)
        //    {
        //        // --- 2.1 Selección del jugo (obligatoria, una por pasada) ---
        //        CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple jugoSel;
        //        using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
        //        {
        //            frmJ.CantidadRequerida = 1; // UNA selección por vuelta
        //            frmJ.ProductoBaseTexto = $"1 x {(prod.Descripcion ?? prod.Codigo)}  ({i}/{cantidad})";
        //            frmJ.ListaPrecio = "001";

        //            if (frmJ.ShowDialog(this) != DialogResult.OK) return;

        //            var lista = frmJ.Selecciones ?? new List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
        //            if (lista.Count == 0) return;
        //            jugoSel = lista[0];
        //        }

        //        // Sumamos recargo
        //        extraTotal += jugoSel.PrecioExtra;

        //        // Insertamos el bloque del jugo (sin notas aún) en el control
        //        item.AddJugoUnidad(jugoSel.Descripcion, jugoSel.PrecioExtra, null);

        //        // --- 2.2 Notas de bebidas (opcional) para esta unidad ---
        //        using (var frmB = new CapaPresentacion.Notas.frmNBebidas())
        //        {
        //            // Si quisieras mostrar referencia arriba, podrías precargar:
        //            // frmB.TextoInicial = ""; // lo dejamos vacío para que no duplique encabezados
        //            if (frmB.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(frmB.Notas))
        //            {
        //                // Agrega las notas a ESTE jugo (el último añadido)
        //                item.AppendNotasAlUltimoJugo(frmB.Notas);
        //            }
        //        }
        //    }

        //    // 3) Calcular PU final = base + promedio de extras por unidad
        //    decimal extraPromedio = item.GetExtraPromedioPorUnidad(cantidad);
        //    decimal puFinal = PrecioDe(prod) + extraPromedio;

        //    // 4) Encabezado del combo + pintar en la lista
        //    item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puFinal);

        //    flpLineas.SuspendLayout();
        //    flpLineas.Controls.Add(item);
        //    flpLineas.ResumeLayout();

        //    // Seleccionar la línea recién agregada
        //    item.SeleccionarEste();
        //    btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);
        //}

        //private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        //{
        //    if (prod == null || cantidad <= 0) return;

        //    // 1) Recolectar (Jugo + Notas) por cada unidad solicitada
        //    var unidades = new List<(string desc, decimal extra, string notas)>();

        //    for (int i = 1; i <= cantidad; i++)
        //    {
        //        // --- 1.1 Jugo (una unidad por pasada) ---
        //        List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> sel = null;

        //        using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
        //        {
        //            frmJ.CantidadRequerida = 1; // una unidad
        //            frmJ.ListaPrecio = "001";
        //            frmJ.ProductoBaseTexto = string.Format("{0} x {1}  ({2}/{3})",
        //                                    1, (prod.Descripcion ?? prod.Codigo), i, cantidad);

        //            if (frmJ.ShowDialog(this) != DialogResult.OK) return;

        //            sel = frmJ.Selecciones ?? new List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
        //            if (sel.Count == 0) return; // canceló o no eligió
        //        }

        //        var j = sel[0]; // solo una selección por pasada

        //        // --- 1.2 Notas de bebidas (opcionales) ---
        //        string notasBebidas = string.Empty;
        //        using (var frmB = new CapaPresentacion.Notas.frmNBebidas())
        //        {
        //            // Si quieres precargar algo, puedes dejarlo en blanco por unidad
        //            // frmB.TextoInicial = "";
        //            if (frmB.ShowDialog(this) == DialogResult.OK)
        //                notasBebidas = frmB.Notas ?? string.Empty;
        //        }

        //        unidades.Add((j.Descripcion, j.PrecioExtra, notasBebidas));
        //    }

        //    // 2) Precio final por unidad = base del desayuno + (promedio de extras por unidad)
        //    decimal extraTotal = unidades.Sum(u => u.extra);
        //    decimal puConExtra = PrecioDe(prod) + (cantidad > 0 ? (extraTotal / cantidad) : 0m);

        //    // 3) Crear el ComboPedidoItem y setear cabecera
        //    var item = new CapaPresentacion.Controles.ComboPedidoItem();
        //    item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puConExtra);

        //    // 4) Agregar un textbox (clonado) por cada unidad, con sus notas debajo
        //    foreach (var u in unidades)
        //    {
        //        var header = new StringBuilder();
        //        header.Append("1 x ").Append(u.desc);
        //        if (u.extra > 0m)
        //            header.Append(" = S/ ").Append(u.extra.ToString("0.00", CultureInfo.InvariantCulture));

        //        // crea el bloque/textarea del jugo
        //        item.AddJugoDesayuno(header.ToString());

        //        // añade notas (una por línea) al último jugo
        //        if (!string.IsNullOrWhiteSpace(u.notas))
        //        {
        //            var lines = (u.notas ?? string.Empty)
        //                        .Replace("\r\n", "\n")
        //                        .Split('\n')
        //                        .Select(s => (s ?? string.Empty).Trim())
        //                        .Where(s => s.Length > 0);

        //            foreach (var ln in lines)
        //                item.AppendNotaAlUltimoJugo(ln);
        //        }
        //    }

        //    // 5) Mostrar el item en el panel izquierdo
        //    flpLineas.SuspendLayout();
        //    flpLineas.Controls.Add(item);
        //    flpLineas.ResumeLayout();

        //    // Seleccionar recién agregado y habilitar Eliminar
        //    item.SeleccionarEste();
        //    btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);
        //}

        // ===== flujo completo por unidad: (Jugo + Notas) y luego Bebida Caliente =====
        //private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        //{
        //    if (prod == null || cantidad <= 0) return;

        //    // 1) Crea el Item del combo (lo mostraremos y luego actualizamos el PU con extras)
        //    var item = new CapaPresentacion.Controles.ComboPedidoItem();
        //    item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, PrecioDe(prod));   // PU base primero

        //    // 2) Por cada desayuno, pedir: Jugo + Notas, y luego Bebida Caliente
        //    for (int i = 1; i <= cantidad; i++)
        //    {
        //        // --- 2.1 JUGO (una unidad) ---
        //        List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> selJ = null;
        //        using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
        //        {
        //            frmJ.CantidadRequerida = 1;
        //            frmJ.ListaPrecio = "001";
        //            frmJ.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}  ({i}/{cantidad})";

        //            if (frmJ.ShowDialog(this) != DialogResult.OK) return;

        //            selJ = frmJ.Selecciones ?? new List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
        //            if (selJ.Count == 0) return;
        //        }

        //        var j = selJ[0];

        //        // --- 2.2 NOTAS BEBIDAS FRÍAS para este jugo (opcional) ---
        //        string notasBebidas = string.Empty;
        //        using (var frmNB = new CapaPresentacion.Notas.frmNBebidas())
        //        {
        //            // precarga vacío por unidad
        //            if (frmNB.ShowDialog(this) == DialogResult.OK)
        //                notasBebidas = frmNB.Notas ?? string.Empty;
        //        }

        //        // agrega al item (agrupa iguales)
        //        item.AddJugoUnidad(j.Descripcion, j.PrecioExtra, notasBebidas);

        //        // --- 2.3 BEBIDA CALIENTE (una unidad) ---
        //        List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple> selB = null;
        //        using (var frmB = new CapaPresentacion.Notas.frmCBebidasCalientes())
        //        {
        //            frmB.CantidadRequerida = 1;
        //            frmB.ListaPrecio = "001";
        //            frmB.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}  ({i}/{cantidad})";

        //            if (frmB.ShowDialog(this) != DialogResult.OK) return;

        //            selB = frmB.Selecciones ?? new List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple>();
        //            if (selB.Count == 0) return;
        //        }

        //        var b = selB[0];

        //        // agrega bebida caliente (si quieres permitir notas separadas, aquí podrías abrir otro diálogo)
        //        item.AddBebidaUnidad(b.Descripcion, b.PrecioExtra, null);
        //    }

        //    // 3) Recalcular PU = base + promedio de TODOS los extras (jugos + bebidas)
        //    decimal puConExtra = PrecioDe(prod) + item.GetExtraPromedioTotal(cantidad);
        //    item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puConExtra); // actualiza cabecera

        //    // 4) Agregar al panel izquierdo
        //    flpLineas.SuspendLayout();
        //    flpLineas.Controls.Add(item);
        //    flpLineas.ResumeLayout();

        //    CapaPresentacion.Controles.ComboPedidoItem.Seleccionar(item, true);
        //    btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);
        //}

        // frmMenuPrincipal.cs  (REEMPLAZA TU MÉTODO)
        //private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        //{
        //    if (prod == null || cantidad <= 0) return;

        //    // 0) Crear el item de combo con PU base (sin extras) y mostrarlo ya en la lista
        //    var item = new CapaPresentacion.Controles.ComboPedidoItem();
        //    decimal puBase = PrecioDe(prod);
        //    item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puBase);

        //    flpLineas.SuspendLayout();
        //    flpLineas.Controls.Add(item);
        //    flpLineas.ResumeLayout();
        //    CapaPresentacion.Controles.ComboPedidoItem.Seleccionar(item, true);
        //    btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);

        //    // 1) JUGOS POR UNIDAD (cada uno con su nota)
        //    for (int i = 1; i <= cantidad; i++)
        //    {
        //        // 1.1) Elegir el jugo (UNA unidad en cada pasada)
        //        List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple> sel = null;
        //        using (var frmJ = new CapaPresentacion.Notas.frmCJugoDesayuno())
        //        {
        //            frmJ.CantidadRequerida = 1; // una unidad por pasada
        //            frmJ.ProductoBaseTexto = $"1 x {prod.Descripcion}  ({i}/{cantidad})";
        //            frmJ.ListaPrecio = "001";

        //            if (frmJ.ShowDialog(this) != DialogResult.OK)
        //            {
        //                // Canceló → deshacer y salir
        //                flpLineas.Controls.Remove(item);
        //                item.Dispose();
        //                return;
        //            }

        //            sel = frmJ.Selecciones ?? new List<CapaPresentacion.Notas.frmCJugoDesayuno.SeleccionSimple>();
        //            if (sel.Count == 0)
        //            {
        //                flpLineas.Controls.Remove(item);
        //                item.Dispose();
        //                return;
        //            }
        //        }

        //        var j = sel[0]; // debe venir justo 1
        //        item.AddJugoUnidad(j.Descripcion, j.PrecioExtra, null);

        //        // 1.2) Nota para este jugo
        //        using (var frmN = new CapaPresentacion.Notas.frmNBebidas())
        //        {
        //            // Precargo el encabezado "1 x JUGO ..."
        //            frmN.TextoInicial = $"1 x {j.Descripcion}";
        //            if (frmN.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(frmN.Notas))
        //            {
        //                item.AppendNotasAlUltimoJugo(frmN.Notas);
        //            }
        //        }
        //    }

        //    // 2) BEBIDAS CALIENTES (después de terminar TODOS los jugos)
        //    using (var frmB = new CapaPresentacion.Notas.frmCBebidasCalientes())
        //    {
        //        frmB.CantidadRequerida = cantidad;                               // exige tantas bebidas como desayunos
        //        frmB.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}";     // visible arriba
        //        frmB.ListaPrecio = "001";

        //        if (frmB.ShowDialog(this) != DialogResult.OK)
        //        {
        //            // Canceló → deshacer y salir
        //            flpLineas.Controls.Remove(item);
        //            item.Dispose();
        //            return;
        //        }

        //        var bebidas = frmB.Selecciones ?? new List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple>();
        //        if (bebidas.Count != cantidad)
        //        {
        //            // Reglas: deben ser exactamente 'cantidad' bebidas
        //            MessageBox.Show($"Debes elegir {cantidad} bebida(s) caliente(s).", "Bebidas", MessageBoxButtons.OK, MessageBoxIcon.Information);
        //            flpLineas.Controls.Remove(item);
        //            item.Dispose();
        //            return;
        //        }

        //        foreach (var b in bebidas)
        //            item.AddBebidaUnidad(b.Descripcion, b.PrecioExtra, null);
        //    }

        //    // 3) Recalcular PU final con TODOS los extras (jugos + bebidas)
        //    decimal extraPromedio = item.GetExtraPromedioTotal(cantidad);
        //    item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puBase + extraPromedio);

        //    // Seleccionar y listo
        //    CapaPresentacion.Controles.ComboPedidoItem.Seleccionar(item, true);
        //    btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);
        //}
        private void EjecutarWizardDesayunoPorUnidad(ceProductos prod, int cantidad)
        {
            if (prod == null || cantidad <= 0) return;

            // 0) Crear item visual y configurar preferencias de agrupado
            var item = new CapaPresentacion.Controles.ComboPedidoItem
            {
                AgruparJugosIguales = true,   // <<— clave para que salga: 2 x JUGO DE PIÑA + notas
                AgruparBebidasIguales = true
            };

            // 1) Por cada unidad: un jugo + (opcional) notas libres para ese jugo
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

                var jugo = sel[0];

                string notasJugo = string.Empty;
                using (var frmN = new CapaPresentacion.Notas.frmNBebidas())
                {
                    // si quieres precargar algo en el cuadro, ponlo aquí:
                    // frmN.TextoInicial = "";
                    if (frmN.ShowDialog(this) == DialogResult.OK)
                        notasJugo = frmN.Notas ?? string.Empty;
                }

                // NO forzar individual: así respeta AgruparJugosIguales = true
                item.AddJugoUnidad(jugo.Descripcion, jugo.PrecioExtra, notasJugo, /*forzarIndividual*/ null);
            }

            // 2) Elegir Bebidas calientes para el total (bloque)
            using (var frmB = new CapaPresentacion.Notas.frmCBebidasCalientes())
            {
                frmB.CantidadRequerida = cantidad;
                frmB.ListaPrecio = "001";
                frmB.ProductoBaseTexto = $"{cantidad} x {prod.Descripcion}";

                if (frmB.ShowDialog(this) == DialogResult.OK)
                {
                    // Asegura tipos compatibles (por si la clase de SeleccionSimple difiere)
                    var seleB = frmB.Selecciones ?? new List<CapaPresentacion.Notas.frmCBebidasCalientes.SeleccionSimple>();
                    foreach (var b in seleB)
                    {
                        item.AddBebidaUnidad(b.Descripcion, b.PrecioExtra, /*notas*/ string.Empty, /*forzarIndividual*/ false);
                    }
                }
                else
                {
                    // Si las bebidas son obligatorias, puedes abortar aquí.
                    // return;
                }
            }

            // 3) Precio final por unidad = base + promedio de extras (jugos + bebidas)
            decimal puBase = PrecioDe(prod);
            decimal puFinal = puBase + item.GetExtraPromedioTotalPorUnidad(cantidad);

            // 4) Encabezado del combo y pintar en el panel izquierdo
            item.SetCombo(prod.Codigo, prod.Descripcion, cantidad, puFinal);

            flpLineas.SuspendLayout();
            flpLineas.Controls.Add(item);
            flpLineas.ResumeLayout();

            CapaPresentacion.Controles.ComboPedidoItem.Seleccionar(item, true);
            btnEliminar.Enabled = (CapaPresentacion.Controles.ComboPedidoItem.SeleccionActual != null);
        }

        ////private string ConstruirResumenJugoPorUnidad(List<UnidadJugo> unis, int cantidad)
        ////{
        ////    if (unis == null || unis.Count == 0) return string.Empty;

        ////    // ¿Todos los jugos son la misma descripción?
        ////    bool todosIguales = true;
        ////    string refDesc = unis[0].Descripcion ?? "";
        ////    for (int i = 1; i < unis.Count; i++)
        ////    {
        ////        if (!string.Equals(refDesc, unis[i].Descripcion ?? "", StringComparison.OrdinalIgnoreCase))
        ////        {
        ////            todosIguales = false;
        ////            break;
        ////        }
        ////    }

        ////    var sb = new System.Text.StringBuilder();

        ////    if (todosIguales)
        ////    {
        ////        // Ej: "2 x JUGO DE PIÑA"
        ////        sb.AppendFormat("{0} x {1}", cantidad, refDesc).AppendLine();

        ////        // Debajo, todas las notas (unidad 1, 2, ...)
        ////        for (int i = 0; i < unis.Count; i++)
        ////        {
        ////            var notas = PartirLineas(unis[i].Notas);
        ////            foreach (var linea in notas)
        ////                if (!string.IsNullOrWhiteSpace(linea))
        ////                    sb.Append("  - ").Append(linea.Trim()).AppendLine();
        ////        }
        ////    }
        ////    else
        ////    {
        ////        // Una sección por unidad:
        ////        for (int i = 0; i < unis.Count; i++)
        ////        {
        ////            var u = unis[i];
        ////            if (u.PrecioExtra > 0m)
        ////                sb.AppendFormat("1 x {0} = S/ {1:0.00}", u.Descripcion, u.PrecioExtra).AppendLine();
        ////            else
        ////                sb.AppendFormat("1 x {0}", u.Descripcion).AppendLine();

        ////            var notas = PartirLineas(u.Notas);
        ////            foreach (var linea in notas)
        ////                if (!string.IsNullOrWhiteSpace(linea))
        ////                    sb.Append("  - ").Append(linea.Trim()).AppendLine();
        ////        }
        ////    }

        ////    return sb.ToString().TrimEnd();
        ////}

        // Split seguro de notas en líneas (sin perder saltos)
        private static IEnumerable<string> PartirLineas(string t)
        {
            if (string.IsNullOrEmpty(t)) yield break;
            t = t.Replace("\r\n", "\n");
            var partes = t.Split('\n');
            for (int i = 0; i < partes.Length; i++) yield return partes[i];
        }

    }
}
