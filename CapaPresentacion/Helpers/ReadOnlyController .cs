using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CapaPresentacion.Helpers
{
    public class ReadOnlyController 
    {
        public static void ActivarSoloLectura(Form form, Control container, Control btnEliminar, Control btnComentario)
        {
            if (form != null)
            {
                form.KeyPreview = true;
                form.KeyDown -= Form_KeyDownSoloLectura;
                form.KeyDown += Form_KeyDownSoloLectura;
            }

            if (btnEliminar != null) btnEliminar.Enabled = false;
            if (btnComentario != null) btnComentario.Enabled = false;

            CongelarControlesDeLinea(container);
        }

        // 👉 sobrecarga simple (sin form)
        public static void ActivarSoloLectura(Control container, Control btnEliminar, Control btnComentario)
        {
            ActivarSoloLectura(null, container, btnEliminar, btnComentario);
        }

        private static void Form_KeyDownSoloLectura(object sender, KeyEventArgs e)
        {
            // Bloquea DEL, F2 y CTRL+E
            if (e.KeyCode == Keys.Delete || e.KeyCode == Keys.F2 || (e.Control && e.KeyCode == Keys.E))
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
            }
        }

        private static void CongelarControlesDeLinea(Control root)
        {
            if (root == null) return;

            foreach (Control c in root.Controls)
            {
                // Intenta poner en solo lectura si el control tiene algo expuesto
                TrySetReadOnlyTrue(c);

                // Recurse
                CongelarControlesDeLinea(c);
            }
        }

        private static void TrySetReadOnlyTrue(object obj)
        {
            try
            {
                var t = obj.GetType();

                // Propiedad ReadOnly { set; }
                var p = t.GetProperty("ReadOnly", BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.CanWrite)
                {
                    p.SetValue(obj, true, null);
                    return;
                }

                // Método SetReadOnly(bool)
                var m = t.GetMethod("SetReadOnly", BindingFlags.Public | BindingFlags.Instance);
                if (m != null)
                {
                    m.Invoke(obj, new object[] { true });
                    return;
                }

                // Método Bloquear()
                var m2 = t.GetMethod("Bloquear", BindingFlags.Public | BindingFlags.Instance);
                if (m2 != null)
                {
                    m2.Invoke(obj, null);
                    return;
                }
            }
            catch { /* silenciar */ }
        }
    }
}
