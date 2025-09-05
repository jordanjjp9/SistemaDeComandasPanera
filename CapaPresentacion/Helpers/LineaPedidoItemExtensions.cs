using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CapaPresentacion.Controles;     // LineaPedidoItem
using CapaPresentacion.Helpers;       // LineaSelection

namespace CapaPresentacion.Helpers
{
    public static class LineaPedidoItemExtensions
    {
        // ============ API PRINCIPAL ============

        /// Crea, configura y agrega una nueva línea al FlowLayoutPanel.
        /// Opcionalmente la deja seleccionada y hace scroll hacia ella.
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

            item.Configurar(codigo, descripcion, cantidad, precioUnitario, notas ?? string.Empty);

            flp.SuspendLayout();
            flp.Controls.Add(item);
            flp.ResumeLayout(performLayout: true);

            if (seleccionar)
                LineaSelection.Select(item, scrollIntoView);

            return item;
        }

        /// Devuelve la línea actualmente seleccionada (si pertenece a este panel).
        public static LineaPedidoItem GetSeleccion(this FlowLayoutPanel flp)
        {
            var sel = LineaSelection.Actual as LineaPedidoItem;
            return (sel != null && ReferenceEquals(sel.Parent, flp)) ? sel : null;
        }

        /// Selecciona la última línea del panel (si existe) y hace scroll a ella.
        public static void SelectLast(this FlowLayoutPanel flp, bool scrollIntoView = true)
        {
            var last = flp.Items().LastOrDefault();
            if (last == null)
            {
                LineaSelection.Clear();
                return;
            }

            LineaSelection.Select(last, scrollIntoView);
        }

        /// Elimina la línea actualmente seleccionada (si está en este panel).
        /// Después selecciona la última que quede.
        public static void RemoveSelected(this FlowLayoutPanel flp)
        {
            var sel = flp.GetSeleccion();
            if (sel == null) return;

            flp.SuspendLayout();
            try
            {
                flp.Controls.Remove(sel);
                sel.Dispose();

                var last = flp.Items().LastOrDefault();
                if (last != null)
                    LineaSelection.Select(last, true);
                else
                    LineaSelection.Clear();
            }
            finally
            {
                flp.ResumeLayout(true);
            }
        }

        /// Devuelve todas las líneas del panel.
        public static IEnumerable<LineaPedidoItem> Items(this FlowLayoutPanel flp)
        {
            foreach (Control c in flp.Controls)
                if (c is LineaPedidoItem li) yield return li;
        }

        /// Ajusta el ancho de todas las líneas cuando cambie el tamaño del panel.
        /// Llama a esto en el Load del form:
        ///     flpLineas.HookAutoWidth();
        public static void HookAutoWidth(this FlowLayoutPanel flp)
        {
            if (flp == null) return;

            flp.SizeChanged -= Flp_SizeChanged_AutoWidth;
            flp.SizeChanged += Flp_SizeChanged_AutoWidth;

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
            int usable = flp.ClientSize.Width
                       - flp.Padding.Left - flp.Padding.Right
                       - item.Margin.Horizontal;

            if (usable < 50) usable = 50;
            item.Width = usable;
        }
    }
}
