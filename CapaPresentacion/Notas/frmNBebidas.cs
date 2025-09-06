using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace CapaPresentacion.Notas
{
    public partial class frmNBebidas : Form
    {
        // ======== Entradas / salidas ========
        public string ProductoBaseTexto { get; set; } = string.Empty;
        public string TextoInicial { get; set; } = string.Empty;
        public string Notas { get; private set; } = string.Empty;

        /// <summary>Cuántas bebidas calientes se omiten porque aquí se eligió “GRANDE”. (0 o 1)</summary>
        public int CuposCalienteConsumidos { get; private set; } = 0;

        // ======== Internos ========
        private TextBoxBase _txtNotas;     // TextBox / RichTextBox real si existe
        private Control _txtNotasCtrl;     // fallback (p. ej., Guna2TextBox)
        private readonly List<bool> _lineaConsumeCupo = new List<bool>(); // para deshacer “GRANDE” con Eliminar

        // Identificación del botón "GRANDE"
        private const string NAME_GRANDE = "btnGrd";
        private const string TEXT_GRANDE = "GRANDE";

        public frmNBebidas()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.CenterParent;
            Load += frmNBebidas_Load;
        }

        private void frmNBebidas_Load(object sender, EventArgs e)
        {
            // 1) Resolver el área de notas
            _txtNotas = FindNotasTextBox();
            if (_txtNotas == null)
                _txtNotasCtrl = FindNotasControl();

            // 2) Botones principales
            var btnContinuar = this.Controls.Find("btnContinuar", true).OfType<Button>().FirstOrDefault();
            var btnEliminar = this.Controls.Find("btnEliminar", true).OfType<Button>().FirstOrDefault();

            if (btnContinuar != null)
            {
                btnContinuar.Click -= btnContinuar_Click;
                btnContinuar.Click += btnContinuar_Click;
                this.AcceptButton = btnContinuar;
            }
            if (btnEliminar != null)
            {
                btnEliminar.Click -= btnEliminar_Click;
                btnEliminar.Click += btnEliminar_Click;
            }

            // 3) Encabezado con producto base (opcional)
            var txtProd = this.Controls.Find("txtProductoSelect", true).FirstOrDefault();
            if (txtProd != null && !string.IsNullOrWhiteSpace(ProductoBaseTexto))
                txtProd.Text = ProductoBaseTexto;

            // 4) Precarga de notas (opcional)
            if (!string.IsNullOrEmpty(TextoInicial))
                TrySetNotasText(TextoInicial);

            // 5) Enganchar TODOS los “chips” (botones), excluyendo Continuar/Eliminar
            WireQuickNoteButtons(this, btnContinuar, btnEliminar);
        }

        // ================== Botones principales ==================
        private void btnContinuar_Click(object sender, EventArgs e)
        {
            Notas = ReadNotasText();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnEliminar_Click(object sender, EventArgs e)
        {
            // Elimina la última línea no vacía y, si consumía cupo (GRANDE), lo devuelve
            var lines = GetLines();
            if (lines.Length == 0) return;

            int i = lines.Length - 1;
            while (i >= 0 && string.IsNullOrWhiteSpace(lines[i])) i--;
            if (i < 0) return;

            bool consumia = false;
            if (_lineaConsumeCupo.Count > 0)
            {
                consumia = _lineaConsumeCupo[_lineaConsumeCupo.Count - 1];
                _lineaConsumeCupo.RemoveAt(_lineaConsumeCupo.Count - 1);
            }

            var nuevas = (i == 0) ? new string[0] : lines.Take(i).ToArray();
            SetLines(nuevas);

            if (consumia && CuposCalienteConsumidos > 0)
                CuposCalienteConsumidos--;
        }

        // ================== Chips (opciones rápidas) ==================
        private void WireQuickNoteButtons(Control root, Button btnContinuar, Button btnEliminar)
        {
            if (root == null) return;

            foreach (Control c in root.Controls)
            {
                WireQuickNoteButtons(c, btnContinuar, btnEliminar);

                bool esBoton =
                    (c is Button) ||
                    (c.GetType().Name.IndexOf("Button", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    ((c.Name ?? "").StartsWith("btn", StringComparison.OrdinalIgnoreCase));

                if (!esBoton) continue;

                if (ReferenceEquals(c, btnContinuar) || ReferenceEquals(c, btnEliminar)) continue;

                c.Click -= Chip_Click;
                c.Click += Chip_Click;
            }
        }

        private void Chip_Click(object sender, EventArgs e)
        {
            var c = sender as Control;
            if (c == null) return;

            bool esGrande = IsGrandeControl(c);
            if (esGrande)
            {
                if (CuposCalienteConsumidos >= 1)
                {
                    System.Media.SystemSounds.Beep.Play();
                    return; // solo se permite un GRANDE
                }
                CuposCalienteConsumidos += 1;
            }

            string actual = ReadNotasText() ?? string.Empty;
            if (actual.Length > 0 && !actual.EndsWith(Environment.NewLine))
                actual += Environment.NewLine;

            string textoChip = (c.Text ?? string.Empty).Trim();
            string nuevaLinea = "- " + textoChip;

            TrySetNotasText(actual + nuevaLinea + Environment.NewLine);

            _lineaConsumeCupo.Add(esGrande);
        }

        // ================== Helpers de texto ==================
        private string ReadNotasText()
        {
            if (_txtNotas != null) return _txtNotas.Text;
            if (_txtNotasCtrl != null) return _txtNotasCtrl.Text ?? string.Empty;
            return string.Empty;
        }

        private void TrySetNotasText(string s)
        {
            if (s == null) s = string.Empty;   // <- compatible con C# 7.3

            if (_txtNotas != null)
            {
                _txtNotas.Text = s;
                _txtNotas.SelectionStart = _txtNotas.TextLength;
                _txtNotas.ScrollToCaret();
                _txtNotas.Focus();
            }
            else if (_txtNotasCtrl != null)
            {
                _txtNotasCtrl.Text = s;
                _txtNotasCtrl.Focus();
            }
        }

        private string[] GetLines()
        {
            var t = ReadNotasText() ?? string.Empty;
            t = t.Replace("\r\n", "\n").Replace("\r", "\n");
            return t.Split('\n');
        }

        private void SetLines(string[] lines)
        {
            string joined = string.Join(Environment.NewLine, lines ?? new string[0]);
            TrySetNotasText(joined);
        }

        // ================== Localización de controles ==================
        private TextBoxBase FindNotasTextBox()
        {
            var candidatos = new[]
            {
                "txtNotasBebida", "txtNotasBCalient", "txtNBebidas",
                "txtNotas", "txtNota", "txtComentarios", "txtComentLibr"
            };

            foreach (var name in candidatos)
            {
                var tb = this.Controls.Find(name, true).OfType<TextBoxBase>().FirstOrDefault();
                if (tb != null) return tb;
            }

            return this.Controls.OfType<TextBoxBase>().FirstOrDefault(t => t.Multiline);
        }

        private Control FindNotasControl()
        {
            var candidatos = new[]
            {
                "txtNotasBebida", "txtNotasBCalient", "txtNBebidas",
                "txtNotas", "txtNota", "txtComentarios", "txtComentLibr"
            };

            foreach (var name in candidatos)
            {
                var c = this.Controls.Find(name, true).FirstOrDefault();
                if (c != null) return c;
            }

            return this.Controls.Cast<Control>().FirstOrDefault(ctrl => ctrl.GetType().GetProperty("Text") != null);
        }

        // ================== Detección de “GRANDE” ==================
        private static bool IsGrandeControl(Control c)
        {
            if (c == null) return false;

            if (string.Equals(c.Name, NAME_GRANDE, StringComparison.OrdinalIgnoreCase))
                return true;

            var txt = (c.Text ?? string.Empty).Trim().ToUpperInvariant();
            if (txt == TEXT_GRANDE) return true;

            return false;
        }

        private void btnCerrar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}