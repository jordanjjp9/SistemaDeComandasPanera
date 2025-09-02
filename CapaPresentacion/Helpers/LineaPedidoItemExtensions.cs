using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CapaPresentacion.Controles;

namespace CapaPresentacion.Helpers
{
    public static class LineaPedidoItemExtensions
    {
        // ============ API PRINCIPAL ============

        /// <summary>
        /// Crea, configura y agrega una nueva línea al FlowLayoutPanel.
        /// Opcionalmente la deja seleccionada y hace scroll hacia ella.
        /// </summary>
        public static LineaPedidoItem AddLinea(this FlowLayoutPanel flp,
                                               string codigo,
                                               string descripcion,
                                               int cantidad,
                                               decimal precioUnitario,
                                               string notas,
                                               bool seleccionar = true,
                                               bool scrollIntoView = true)
        {
            if (flp == null) throw new ArgumentNullException(nameof(flp));

            var item = new LineaPedidoItem
            {
                Margin = new Padding(6),
            };

            AjustarAncho(flp, item);

            item.Configurar(codigo, descripcion, cantidad, precioUnitario, notas);

            flp.SuspendLayout();
            flp.Controls.Add(item);
            flp.ResumeLayout(performLayout: true);

            if (seleccionar)
            {
                SeleccionarExclusivo(item);
                if (scrollIntoView)
                    flp.ScrollControlIntoView(item);
            }

            return item;
        }

        /// <summary>
        /// Devuelve la línea actualmente seleccionada (si pertenece a este panel).
        /// </summary>
        public static LineaPedidoItem GetSeleccion(this FlowLayoutPanel flp)
        {
            var sel = LineaPedidoItem.SeleccionActual;
            return (sel != null && sel.Parent == flp) ? sel : null;
        }

        /// <summary>
        /// Selecciona la última línea del panel (si existe) y hace scroll a ella.
        /// </summary>
        public static void SelectLast(this FlowLayoutPanel flp, bool scrollIntoView = true)
        {
            var last = flp.Items().LastOrDefault();
            if (last == null) return;

            SeleccionarExclusivo(last);
            if (scrollIntoView)
                flp.ScrollControlIntoView(last);
        }

        /// <summary>
        /// Elimina la línea actualmente seleccionada (si está en este panel).
        /// Después selecciona la última que quede.
        /// </summary>
        public static void RemoveSelected(this FlowLayoutPanel flp)
        {
            var sel = flp.GetSeleccion();
            if (sel == null) return;

            flp.SuspendLayout();
            try
            {
                int idx = flp.Controls.GetChildIndex(sel);
                flp.Controls.Remove(sel);
                sel.Dispose();

                // Selecciona la nueva última
                var last = flp.Items().LastOrDefault();
                if (last != null)
                {
                    SeleccionarExclusivo(last);
                    flp.ScrollControlIntoView(last);
                }
                else
                {
                    // No hay elementos -> deselección visual
                    LimpiarSeleccionExclusiva();
                }
            }
            finally
            {
                flp.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Devuelve todas las líneas del panel.
        /// </summary>
        public static IEnumerable<LineaPedidoItem> Items(this FlowLayoutPanel flp)
        {
            foreach (Control c in flp.Controls)
                if (c is LineaPedidoItem li) yield return li;
        }

        /// <summary>
        /// Ajusta el ancho de todas las líneas cuando cambie el tamaño del panel.
        /// Llama a esto en el Load del form:
        /// flpLineas.HookAutoWidth();
        /// </summary>
        public static void HookAutoWidth(this FlowLayoutPanel flp)
        {
            if (flp == null) return;

            flp.SizeChanged -= Flp_SizeChanged_AutoWidth;
            flp.SizeChanged += Flp_SizeChanged_AutoWidth;

            // Ajuste inicial
            AjustarAnchoTodos(flp);
        }

        // ============ EVENTO PARA AUTO-WIDTH ============

        private static void Flp_SizeChanged_AutoWidth(object sender, EventArgs e)
        {
            if (sender is FlowLayoutPanel flp)
                AjustarAnchoTodos(flp);
        }

        // ============ HELPERS DE ANCHO ============

        private static void AjustarAnchoTodos(FlowLayoutPanel flp)
        {
            flp.SuspendLayout();
            try
            {
                foreach (Control c in flp.Controls)
                    if (c is LineaPedidoItem li)
                        AjustarAncho(flp, li);
            }
            finally
            {
                flp.ResumeLayout(true);
            }
        }

        private static void AjustarAncho(FlowLayoutPanel flp, LineaPedidoItem item)
        {
            // Usa todo el ancho disponible del panel, respetando Padding y Margin
            int usable = flp.ClientSize.Width
                       - flp.Padding.Left - flp.Padding.Right
                       - item.Margin.Horizontal;

            if (usable < 50) usable = 50;
            item.Width = usable;
        }

        // ============ SELECCIÓN EXCLUSIVA (reflexión) ============

        /// <summary>
        /// Marca item como seleccionado de forma exclusiva:
        /// desmarca el anterior, actualiza LineaPedidoItem.SeleccionActual
        /// y dispara el evento SeleccionCambio.
        /// </summary>
        private static void SeleccionarExclusivo(LineaPedidoItem item)
        {
            if (item == null) return;

            // 1) Obtener el actual
            var tipo = typeof(LineaPedidoItem);
            var propSel = tipo.GetProperty("SeleccionActual",
                BindingFlags.Public | BindingFlags.Static);
            var selActual = propSel?.GetValue(null, null) as LineaPedidoItem;

            if (ReferenceEquals(selActual, item))
            {
                // Ya está seleccionado, solo asegura el borde
                item.SetVisualSelected(true);
                return;
            }

            // 2) Quitar visual del anterior
            selActual?.SetVisualSelected(false);

            // 3) Poner visual en el nuevo
            item.SetVisualSelected(true);

            // 4) Actualizar SeleccionActual (setter es privado -> usar reflexión)
            var setSel = propSel?.GetSetMethod(true);
            setSel?.Invoke(null, new object[] { item });

            // 5) Disparar SeleccionCambio (evento estático)
            // Nota: los eventos no pueden invocarse fuera del tipo; tomamos el field backing.
            var evtField = tipo.GetField("SeleccionCambio",
                BindingFlags.Static | BindingFlags.NonPublic);
            var del = evtField?.GetValue(null) as EventHandler;
            del?.Invoke(null, EventArgs.Empty);
        }

        /// <summary>
        /// Limpia la selección global (sin seleccionar ningún item).
        /// </summary>
        private static void LimpiarSeleccionExclusiva()
        {
            var tipo = typeof(LineaPedidoItem);
            var propSel = tipo.GetProperty("SeleccionActual",
                BindingFlags.Public | BindingFlags.Static);
            var selActual = propSel?.GetValue(null, null) as LineaPedidoItem;

            selActual?.SetVisualSelected(false);

            var setSel = propSel?.GetSetMethod(true);
            setSel?.Invoke(null, new object[] { null });

            var evtField = tipo.GetField("SeleccionCambio",
                BindingFlags.Static | BindingFlags.NonPublic);
            var del = evtField?.GetValue(null) as EventHandler;
            del?.Invoke(null, EventArgs.Empty);
        }
    }
}
