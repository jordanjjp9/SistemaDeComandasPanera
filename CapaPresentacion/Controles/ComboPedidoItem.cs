using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using CapaPresentacion.Helpers;   // ILineaSeleccionable y LineaSelection

namespace CapaPresentacion.Controles
{
    public partial class ComboPedidoItem : UserControl, ILineaSeleccionable
    {
        // ===== Encabezado del combo =====
        public string Codigo { get; private set; } = "";
        public string Descripcion { get; private set; } = "";
        public int Cantidad { get; private set; } = 1;
        public decimal PrecioUnitario { get; private set; } = 0m;   // PU final del combo (CON IGV)
        public decimal Total { get { return Cantidad * PrecioUnitario; } }
        public string NotasEncabezado { get; private set; } = string.Empty;

        // ======== Export plano ========
        public sealed class LineaExport
        {
            public string Codigo { get; set; }                 // CDG_PROD (10 dígitos lo pone el DAO)
            public string Descripcion { get; set; }
            public int Cantidad { get; set; }
            public decimal PrecioUnitarioConIgv { get; set; }   // CON IGV (para extras es el recargo)
            public string Notas { get; set; }                   // OBS_PPRD
        }

        private readonly List<LineaExport> _planas = new List<LineaExport>();
        //   public IEnumerable<LineaExport> GetLineasExport() { return _planas.ToArray(); }

        public IEnumerable<LineaExport> GetLineasExport()
        {
            // 1) Encabezado del combo (siempre, con su PU real)
            yield return new LineaExport
            {
                Codigo = this.Codigo ?? string.Empty,
                Descripcion = this.Descripcion ?? string.Empty,
                Cantidad = this.Cantidad,
                PrecioUnitarioConIgv = this.PrecioUnitario,
                // Usa tu helper; si no lo tienes, reemplaza por (this.NotasEncabezado ?? "")
                Notas = GetNotasEncabezadoRaw()
            };

            // 2) Jugos (cada uno con PU = 0.00)
            foreach (var j in _jugos)   // _jugos es tu lista interna (BloqueTexto u otro)
            {
                yield return new LineaExport
                {
                    Codigo = j.Codigo ?? string.Empty,
                    Descripcion = j.Descripcion ?? "JUGO",
                    Cantidad = j.Cantidad,
                    PrecioUnitarioConIgv = 0m,
                    Notas = FormatearNotas(j.Notas) // si Notas ya es string, deja j.Notas
                };
            }

            // 3) Bebidas calientes (PU = 0.00)
            foreach (var b in _bebidas)
            {
                yield return new LineaExport
                {
                    Codigo = b.Codigo ?? string.Empty,
                    Descripcion = b.Descripcion ?? "BEBIDA",
                    Cantidad = b.Cantidad,
                    PrecioUnitarioConIgv = 0m,
                    Notas = FormatearNotas(b.Notas)
                };
            }

            // 4) Tamales (PU = 0.00)
            foreach (var t in _tamales)
            {
                yield return new LineaExport
                {
                    Codigo = t.Codigo ?? string.Empty,
                    Descripcion = t.Descripcion ?? "TAMAL",
                    Cantidad = t.Cantidad,
                    PrecioUnitarioConIgv = 0m,
                    Notas = FormatearNotas(t.Notas)
                };
            }
        }

        // Preferencias de agrupación
        public bool AgruparJugosIguales { get; set; } = true;
        public bool AgruparBebidasIguales { get; set; } = true;
        public bool AgruparTamalesIguales { get; set; } = true;

        // ===== Estado UI general =====
        private readonly ToolTip _tt = new ToolTip();
        private int _baseHeight;
        private bool _pendingGrow = false;

        // Plantillas opcionales (creadas en el diseñador)
        private Guna2TextBox _tplJugo;
        private Guna2TextBox _tplBeb;
        private Guna2TextBox _tplTamal;

        // ===== Modelo interno genérico =====
        private sealed class BloqueTexto
        {
            public string Key;
            public string Codigo;          // para exportar CDG_PROD
            public string Descripcion;
            public decimal PrecioExtra;
            public int Cantidad;
            public List<string> Notas = new List<string>();
            public Guna2TextBox TextBox;

            public int ExportIndex = -1;   // índice dentro de _planas para mantener sincronía
        }

        // Listas por tipo
        private readonly List<BloqueTexto> _jugos = new List<BloqueTexto>();
        private readonly List<BloqueTexto> _bebidas = new List<BloqueTexto>();
        private readonly List<BloqueTexto> _tamales = new List<BloqueTexto>();

        // Últimos agregados (para edición rápida)
        private BloqueTexto _ultimoJugo;
        private BloqueTexto _ultimaBebida;
        private BloqueTexto _ultimoTamal;

        // Bloque actualmente seleccionado para editar notas
        private BloqueTexto _bloqueSeleccionado;

        // Notas del ENCABEZADO del combo (debajo de "N x DESCRIPCION = S/ …")
        private readonly List<string> _notasEncabezado = new List<string>();

        // Marca si el usuario tocó el encabezado (txtCombo)
        private bool _encabezadoActivo = false;
        public bool EncabezadoActivo { get { return _encabezadoActivo; } }

        // ===== ILineaSeleccionable =====
        public Control View { get { return this; } }
        public void SetVisualSelected(bool selected)
        {
            this.BorderStyle = selected ? BorderStyle.FixedSingle : BorderStyle.None;
        }

        public ComboPedidoItem()
        {
            InitializeComponent();

            // Tomar plantillas (si existen) y sacarlas de sus contenedores
            _tplJugo = this.Controls.Find("txtJugoDesayuno", true).OfType<Guna2TextBox>().FirstOrDefault();
            _tplBeb = this.Controls.Find("txtBebCDesay", true).OfType<Guna2TextBox>().FirstOrDefault();
            _tplTamal = this.Controls.Find("txtTamal", true).OfType<Guna2TextBox>().FirstOrDefault();

            if (_tplJugo != null) { _tplJugo.Visible = false; if (_tplJugo.Parent is FlowLayoutPanel pj) pj.Controls.Remove(_tplJugo); }
            if (_tplBeb != null) { _tplBeb.Visible = false; if (_tplBeb.Parent is FlowLayoutPanel pb) pb.Controls.Remove(_tplBeb); }
            if (_tplTamal != null) { _tplTamal.Visible = false; if (_tplTamal.Parent is FlowLayoutPanel pt) pt.Controls.Remove(_tplTamal); }

            // Encabezado
            ConfigurarTextBoxSoloLectura(txtCombo);

            // click en el encabezado
            if (txtCombo != null)
            {
                txtCombo.Click -= Header_Click; txtCombo.Click += Header_Click;
                txtCombo.MouseDown -= Header_Click; txtCombo.MouseDown += Header_Click;
            }

            // Preparar contenedores
            PrepFlow(flpJugos);
            FlowLayoutPanel flpB;
            if (TryFindControl("flpBebCDesayuno", out flpB)) PrepFlow(flpB);
            FlowLayoutPanel flpT;
            if (TryFindControl("flpTamal", out flpT)) PrepFlow(flpT);

            // AutoGrow
            if (txtCombo != null) txtCombo.TextChanged += (s, e) => RecalcAutoGrowSafe();
            if (flpJugos != null)
            {
                flpJugos.SizeChanged += (s, e) => RecalcAutoGrowSafe();
                flpJugos.ControlAdded += (s, e) => RecalcAutoGrowSafe();
                flpJugos.ControlRemoved += (s, e) => RecalcAutoGrowSafe();
            }
            if (flpB != null)
            {
                flpB.SizeChanged += (s, e) => RecalcAutoGrowSafe();
                flpB.ControlAdded += (s, e) => RecalcAutoGrowSafe();
                flpB.ControlRemoved += (s, e) => RecalcAutoGrowSafe();
            }
            if (flpT != null)
            {
                flpT.SizeChanged += (s, e) => RecalcAutoGrowSafe();
                flpT.ControlAdded += (s, e) => RecalcAutoGrowSafe();
                flpT.ControlRemoved += (s, e) => RecalcAutoGrowSafe();
            }

            this.SizeChanged += (s, e) => RecalcAutoGrowSafe();

            // Selección global
            WireSelectClick(this);

            // Inner TextBox de txtCombo también marca encabezado activo
            WireInnerTextBoxClicks(txtCombo);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            int hCombo = (txtCombo != null) ? txtCombo.Height : 0;
            int hJugos = (flpJugos != null) ? flpJugos.Height : 0;

            FlowLayoutPanel flpB;
            int hBeb = (TryFindControl("flpBebCDesayuno", out flpB)) ? flpB.Height : 0;

            FlowLayoutPanel flpT;
            int hTam = (TryFindControl("flpTamal", out flpT)) ? flpT.Height : 0;

            _baseHeight = this.Height - (hCombo + hJugos + hBeb + hTam);
            RecalcAutoGrowSafe();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (_pendingGrow) { _pendingGrow = false; RecalcAutoGrow(); }
        }

        // ======= Encabezado =======
        public void SetCombo(string codigo, string descripcion, int cantidad, decimal precioUnitarioFinal)
        {
            Codigo = (codigo ?? string.Empty).Trim();
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? Codigo : descripcion.Trim();
            Cantidad = Math.Max(1, cantidad);
            PrecioUnitario = Math.Max(0m, precioUnitarioFinal);

            // Export: encabezado como primera línea
            _planas.Clear();
            _planas.Add(new LineaExport
            {
                Codigo = Codigo,
                Descripcion = Descripcion,
                Cantidad = Cantidad,
                PrecioUnitarioConIgv = PrecioUnitario,
                Notas = GetNotasEncabezadoRaw()
            });

            RepintarEncabezado();
        }

        private void RepintarEncabezado()
        {
            if (txtCombo == null) return;

            var sb = new StringBuilder();
            sb.AppendFormat("{0} x {1} = S/ {2:0.00}", Cantidad, Descripcion.ToUpperInvariant(), Total);

            foreach (var n in _notasEncabezado)
                sb.AppendLine().Append("  - ").Append(n);

            txtCombo.Text = sb.ToString();
            _tt.SetToolTip(txtCombo, "PU: S/ " + PrecioUnitario.ToString("0.00"));
            RecalcAutoGrowSafe();
        }

        public void AppendNotasEncabezado(string notas)
        {
            if (string.IsNullOrWhiteSpace(notas)) return;
            foreach (var n in ToNotas(notas))
            {
                // dedup simple (case-insensitive)
                bool existe = _notasEncabezado.Any(x => string.Equals(x, n, StringComparison.OrdinalIgnoreCase));
                if (!existe) _notasEncabezado.Add(n);
            }
            SyncHeaderExportNotes();
            RepintarEncabezado();
        }

        public void SetNotasEncabezado(string notas)
        {
            _notasEncabezado.Clear();
            if (!string.IsNullOrWhiteSpace(notas))
                foreach (var n in ToNotas(notas))
                    _notasEncabezado.Add(n);
            SyncHeaderExportNotes();
            RepintarEncabezado();
        }

        private void SyncHeaderExportNotes()
        {
            if (_planas.Count > 0)
                _planas[0].Notas = GetNotasEncabezadoRaw();
        }

        public string GetNotasEncabezadoRaw()
        {
            return FormatearNotas(_notasEncabezado);
        }

        // ======= TAMALES =======
        public void ClearTamales()
        {
            foreach (var b in _tamales) RemoveExportLine(b);

            _tamales.Clear();
            _ultimoTamal = null;
            _bloqueSeleccionado = null;

            FlowLayoutPanel flpT;
            if (TryFindControl("flpTamal", out flpT)) flpT.Controls.Clear();

            RecalcAutoGrowSafe();
        }

        // Compatibilidad (sin código)
        public void AddTamalUnidad(string descripcion, decimal precioExtra, string notas, bool? forzarIndividual)
        { AddTamalUnidad(string.Empty, descripcion, precioExtra, notas, forzarIndividual); }

        // Con código
        public void AddTamalUnidad(string codigo, string descripcion, decimal precioExtra, string notas, bool? forzarIndividual)
        {
            FlowLayoutPanel flpT;
            if (!TryFindControl("flpTamal", out flpT)) return;

            bool individual = (forzarIndividual.HasValue) ? forzarIndividual.Value : !AgruparTamalesIguales;

            BloqueTexto bloque = null;
            if (!individual)
            {
                string key = KeyDe(descripcion, precioExtra);
                bloque = _tamales.FirstOrDefault(x => x.Key == key);
            }

            if (bloque == null)
            {
                bloque = new BloqueTexto
                {
                    Key = KeyDe(descripcion, precioExtra),
                    Codigo = (codigo ?? "").Trim(),
                    Descripcion = (descripcion ?? "").Trim(),
                    PrecioExtra = precioExtra,
                    Cantidad = 1,
                    TextBox = CrearTextBoxDesdePlantilla(_tplTamal, flpT)
                };

                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));

                bloque.TextBox.Text = TextoDe(bloque);
                VincularBloqueVisual(bloque);

                flpT.Controls.Add(bloque.TextBox);
                flpT.PerformLayout();

                _tamales.Add(bloque);
                AddExportLine(bloque);
            }
            else
            {
                bloque.Cantidad += 1;
                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));
                bloque.TextBox.Text = TextoDe(bloque);
                UpdateExportLine(bloque);
            }

            _ultimoTamal = bloque;
            RecalcAutoGrowSafe();
        }

        public void AppendNotasAlUltimoTamal(string notas)
        {
            if (_ultimoTamal == null || string.IsNullOrWhiteSpace(notas)) return;
            _ultimoTamal.Notas.AddRange(ToNotas(notas));
            _ultimoTamal.TextBox.Text = TextoDe(_ultimoTamal);
            UpdateExportLine(_ultimoTamal);
            RecalcAutoGrowSafe();
        }

        // ======= JUGOS =======
        public void ClearJugos()
        {
            foreach (var b in _jugos) RemoveExportLine(b);

            _jugos.Clear();
            _ultimoJugo = null;
            _bloqueSeleccionado = null;
            if (flpJugos != null) flpJugos.Controls.Clear();
            RecalcAutoGrowSafe();
        }

        // Compatibilidad (sin código)
        public void AddJugoUnidad(string descripcion, decimal precioExtra, string notas, bool? forzarIndividual)
        { AddJugoUnidad(string.Empty, descripcion, precioExtra, notas, forzarIndividual); }

        // Con código
        public void AddJugoUnidad(string codigo, string descripcion, decimal precioExtra, string notas, bool? forzarIndividual)
        {
            if (flpJugos == null) return;

            bool individual = (forzarIndividual.HasValue) ? forzarIndividual.Value : !AgruparJugosIguales;

            BloqueTexto bloque = null;
            if (!individual)
            {
                string key = KeyDe(descripcion, precioExtra);
                bloque = _jugos.FirstOrDefault(x => x.Key == key);
            }

            if (bloque == null)
            {
                bloque = new BloqueTexto
                {
                    Key = KeyDe(descripcion, precioExtra),
                    Codigo = (codigo ?? "").Trim(),
                    Descripcion = (descripcion ?? "").Trim(),
                    PrecioExtra = precioExtra,
                    Cantidad = 1,
                    TextBox = CrearTextBoxDesdePlantilla(_tplJugo, flpJugos)
                };

                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));

                bloque.TextBox.Text = TextoDe(bloque);
                VincularBloqueVisual(bloque);

                flpJugos.Controls.Add(bloque.TextBox);
                flpJugos.PerformLayout();

                _jugos.Add(bloque);
                AddExportLine(bloque);
            }
            else
            {
                bloque.Cantidad += 1;
                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));
                bloque.TextBox.Text = TextoDe(bloque);
                UpdateExportLine(bloque);
            }

            _ultimoJugo = bloque;
            RecalcAutoGrowSafe();
        }

        public void AppendNotasAlUltimoJugo(string notas)
        {
            if (_ultimoJugo == null || string.IsNullOrWhiteSpace(notas)) return;
            _ultimoJugo.Notas.AddRange(ToNotas(notas));
            _ultimoJugo.TextBox.Text = TextoDe(_ultimoJugo);
            UpdateExportLine(_ultimoJugo);
            RecalcAutoGrowSafe();
        }

        public decimal GetExtraPromedioJugosPorUnidad(int cantidadTotal)
        {
            if (cantidadTotal <= 0) return 0m;
            decimal extraTotal = 0m;
            foreach (var b in _jugos) extraTotal += (b.PrecioExtra * b.Cantidad);
            return extraTotal / cantidadTotal;
        }

        // ======= BEBIDAS CALIENTES =======
        public void ClearBebidas()
        {
            foreach (var b in _bebidas) RemoveExportLine(b);

            _bebidas.Clear();
            _ultimaBebida = null;
            _bloqueSeleccionado = null;

            FlowLayoutPanel flpB;
            if (TryFindControl("flpBebCDesayuno", out flpB)) flpB.Controls.Clear();

            RecalcAutoGrowSafe();
        }

        // Compatibilidad (sin código)
        public void AddBebidaUnidad(string descripcion, decimal precioExtra, string notas, bool? forzarIndividual)
        { AddBebidaUnidad(string.Empty, descripcion, precioExtra, notas, forzarIndividual); }

        // Con código
        public void AddBebidaUnidad(string codigo, string descripcion, decimal precioExtra, string notas, bool? forzarIndividual)
        {
            FlowLayoutPanel flpB;
            if (!TryFindControl("flpBebCDesayuno", out flpB)) return;

            bool individual = (forzarIndividual.HasValue) ? forzarIndividual.Value : !AgruparBebidasIguales;

            BloqueTexto bloque = null;
            if (!individual)
            {
                string key = KeyDe(descripcion, precioExtra);
                bloque = _bebidas.FirstOrDefault(x => x.Key == key);
            }

            if (bloque == null)
            {
                bloque = new BloqueTexto
                {
                    Key = KeyDe(descripcion, precioExtra),
                    Codigo = (codigo ?? "").Trim(),
                    Descripcion = (descripcion ?? "").Trim(),
                    PrecioExtra = precioExtra,
                    Cantidad = 1,
                    TextBox = CrearTextBoxDesdePlantilla(_tplBeb, flpB)
                };

                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));

                bloque.TextBox.Text = TextoDe(bloque);
                VincularBloqueVisual(bloque);

                flpB.Controls.Add(bloque.TextBox);
                flpB.PerformLayout();

                _bebidas.Add(bloque);
                AddExportLine(bloque);
            }
            else
            {
                bloque.Cantidad += 1;
                if (!string.IsNullOrWhiteSpace(notas))
                    bloque.Notas.AddRange(ToNotas(notas));
                bloque.TextBox.Text = TextoDe(bloque);
                UpdateExportLine(bloque);
            }

            _ultimaBebida = bloque;
            RecalcAutoGrowSafe();
        }

        public void AppendNotasALaUltimaBebida(string notas)
        {
            if (_ultimaBebida == null || string.IsNullOrWhiteSpace(notas)) return;
            _ultimaBebida.Notas.AddRange(ToNotas(notas));
            _ultimaBebida.TextBox.Text = TextoDe(_ultimaBebida);
            UpdateExportLine(_ultimaBebida);
            RecalcAutoGrowSafe();
        }

        public decimal GetExtraPromedioBebidasPorUnidad(int cantidadTotal)
        {
            if (cantidadTotal <= 0) return 0m;
            decimal extraTotal = 0m;
            foreach (var b in _bebidas) extraTotal += (b.PrecioExtra * b.Cantidad);
            return extraTotal / cantidadTotal;
        }

        public decimal GetExtraPromedioTotalPorUnidad(int cantidadTotal)
        {
            return GetExtraPromedioJugosPorUnidad(cantidadTotal) +
                   GetExtraPromedioBebidasPorUnidad(cantidadTotal);
        }

        // ===== Export helpers =====
        private void AddExportLine(BloqueTexto b)
        {
            //var line = new LineaExport
            //{
            //    Codigo = b.Codigo ?? string.Empty,
            //    Descripcion = b.Descripcion,
            //    Cantidad = b.Cantidad,
            //    PrecioUnitarioConIgv = b.PrecioExtra,
            //    Notas = FormatearNotas(b.Notas)
            //};
            //_planas.Add(line);
            //b.ExportIndex = _planas.Count - 1;

            var line = new LineaExport
            {
                Codigo = b.Codigo ?? string.Empty,
                Descripcion = b.Descripcion,
                Cantidad = b.Cantidad,
                // Las sub-líneas SIEMPRE salen con 0.00. El extra ya está incluido en el PU del combo.
                PrecioUnitarioConIgv = 0m,
                Notas = FormatearNotas(b.Notas)
            };
            _planas.Add(line);
            b.ExportIndex = _planas.Count - 1;
        }

        private void UpdateExportLine(BloqueTexto b)
        {
            //if (b.ExportIndex < 0 || b.ExportIndex >= _planas.Count) return;
            //var line = _planas[b.ExportIndex];
            //line.Cantidad = b.Cantidad;
            //line.PrecioUnitarioConIgv = b.PrecioExtra;
            ////line.Notas = FormatearNotas(b.Notes ?? b.Notas); // por compatibilidad
            //line.Notas = FormatearNotas(b.Notas);   // << aquí estaba el error (antes decía b.Notes)

            if (b.ExportIndex < 0 || b.ExportIndex >= _planas.Count) return;
            var line = _planas[b.ExportIndex];
            line.Cantidad = b.Cantidad;
            // Mantener 0.00 en sub-líneas
            line.PrecioUnitarioConIgv = 0m;
            line.Notas = FormatearNotas(b.Notas);
        }

        private void RemoveExportLine(BloqueTexto b)
        {
            if (b.ExportIndex >= 0 && b.ExportIndex < _planas.Count)
            {
                _planas.RemoveAt(b.ExportIndex);

                // reindex simple de todos los bloques
                Action<List<BloqueTexto>> fix = list =>
                {
                    for (int i = 0; i < list.Count; i++)
                        if (list[i].ExportIndex > b.ExportIndex)
                            list[i].ExportIndex--;
                };
                fix(_jugos);
                fix(_bebidas);
                fix(_tamales);
            }
            b.ExportIndex = -1;
        }

        // ===== Selección global =====
        private void WireSelectClick(Control root)
        {
            if (root == null) return;
            root.Click -= Any_Click_Select; root.Click += Any_Click_Select;
            root.MouseDown -= Any_Click_Select; root.MouseDown += Any_Click_Select;
            foreach (Control c in root.Controls) WireSelectClick(c);
        }

        private void WireInnerTextBoxClicks(Control guna2TextBox)
        {
            TextBox inner = TryGetInnerTextBox(guna2TextBox);
            if (inner == null) return;

            inner.ReadOnly = true;
            inner.ShortcutsEnabled = false;
            inner.Cursor = Cursors.Hand;

            // selección global
            inner.Click -= Any_Click_Select; inner.Click += Any_Click_Select;
            inner.MouseDown -= Any_Click_Select; inner.MouseDown += Any_Click_Select;

            // marcar encabezado activo cuando tocan el inner
            inner.Click -= Inner_HeaderMark_Click; inner.Click += Inner_HeaderMark_Click;
            inner.MouseDown -= Inner_HeaderMark_Click; inner.MouseDown += Inner_HeaderMark_Click;
        }

        private void Inner_HeaderMark_Click(object sender, EventArgs e) { MarcarEncabezadoActivo(); }

        private void Header_Click(object sender, EventArgs e) { MarcarEncabezadoActivo(); }

        private void Any_Click_Select(object sender, EventArgs e)
        {
            LineaSelection.Select(this, true);   // selección global única
        }

        // ===== AutoGrow / medidas =====
        private void RecalcAutoGrowSafe()
        {
            if (IsHandleCreated) RecalcAutoGrow();
            else _pendingGrow = true;
        }

        private void RecalcAutoGrow()
        {
            if (!IsHandleCreated) return;

            AjustarAnchosContenedor(flpJugos);

            FlowLayoutPanel flpB;
            if (TryFindControl("flpBebCDesayuno", out flpB))
                AjustarAnchosContenedor(flpB);

            FlowLayoutPanel flpT;
            if (TryFindControl("flpTamal", out flpT))
                AjustarAnchosContenedor(flpT);

            int hCombo = AltoNecesario(txtCombo);
            int hJugos = (flpJugos != null) ? flpJugos.Height : 0;

            int hBeb = 0;
            FlowLayoutPanel flpB2;
            if (TryFindControl("flpBebCDesayuno", out flpB2))
                hBeb = flpB2.Height;

            int hTam = 0;
            FlowLayoutPanel flpT2;
            if (TryFindControl("flpTamal", out flpT2))
                hTam = flpT2.Height;

            SuspendLayout();
            if (txtCombo != null && txtCombo.Height != hCombo) txtCombo.Height = hCombo;
            this.Height = _baseHeight + hCombo + hJugos + hBeb + hTam;
            ResumeLayout();
        }

        private void AjustarAnchosContenedor(FlowLayoutPanel cont)
        {
            if (cont == null) return;

            int ancho = Math.Max(1, cont.ClientSize.Width);

            foreach (Control c in cont.Controls)
            {
                if (c.Width != ancho)
                {
                    c.Width = ancho;
                    AjustarAlturaControl(c);
                }
            }
        }

        private void AjustarAlturaControl(Control c)
        {
            var tb = c as Guna2TextBox;
            if (tb == null) return;

            int w = Math.Max(1, tb.ClientSize.Width);
            using (Graphics g = tb.CreateGraphics())
            {
                var sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.MeasureTrailingSpaces);
                SizeF size = g.MeasureString((tb.Text ?? "") + "\nA", tb.Font, w, sf);
                int needed = (int)Math.Ceiling(size.Height) + 4;
                if (needed < 28) needed = 28;
                if (tb.Height != needed) tb.Height = needed;
            }
        }

        private int AltoNecesario(Control tb)
        {
            if (tb == null) return 0;

            string t = Convert.ToString(tb.GetType().GetProperty("Text").GetValue(tb, null)) ?? string.Empty;
            int w = tb.ClientSize.Width > 0 ? tb.ClientSize.Width : Math.Max(1, tb.Width - 6);

            if (t.Length == 0) return Math.Max(24, tb.Font.Height + 6);

            TextBox inner = TryGetInnerTextBox(tb);
            if (inner != null && inner.IsHandleCreated)
            {
                int last = Math.Max(0, t.Length - 1);
                while (last >= 0 && (t[last] == '\r' || t[last] == '\n')) last--;
                if (last < 0) last = 0;

                try { inner.WordWrap = true; } catch { }

                Point pt = inner.GetPositionFromCharIndex(last);
                int needed = pt.Y + inner.Font.Height + 6;
                return Math.Max(28, needed);
            }

            using (Graphics g = tb.CreateGraphics())
            {
                var sf = new StringFormat(StringFormatFlags.LineLimit | StringFormatFlags.MeasureTrailingSpaces);
                var size = g.MeasureString(t + "\nA", tb.Font, w, sf);
                int needed = (int)Math.Ceiling(size.Height) + 4;
                return Math.Max(28, needed);
            }
        }

        // ===== Helpers visuales / edición =====
        private static void ConfigurarTextBoxSoloLectura(Guna2TextBox tb)
        {
            if (tb == null) return;

            tb.Multiline = true;
            tb.WordWrap = true;
            tb.AcceptsReturn = true;
            tb.ScrollBars = ScrollBars.None;
            tb.ReadOnly = true;
            tb.Enabled = true;
            tb.Cursor = Cursors.Hand;
            tb.Dock = DockStyle.Top;
        }

        private Guna2TextBox CrearTextBoxDesdePlantilla(Guna2TextBox plantilla, FlowLayoutPanel contenedor)
        {
            var tb = new Guna2TextBox();

            if (plantilla != null)
            {
                tb.Font = plantilla.Font;
                tb.ForeColor = plantilla.ForeColor;
                tb.FillColor = plantilla.FillColor;
                tb.BorderRadius = plantilla.BorderRadius;
                tb.BorderColor = plantilla.BorderColor;
                tb.BorderThickness = plantilla.BorderThickness;
                tb.Padding = plantilla.Padding;
                tb.Margin = new Padding(0, 4, 0, 0);
                tb.PlaceholderText = plantilla.PlaceholderText;
                tb.TextAlign = plantilla.TextAlign;
            }
            else
            {
                tb.BorderRadius = 6;
                tb.BorderThickness = 1;
                tb.Margin = new Padding(0, 4, 0, 0);
            }

            tb.Dock = DockStyle.Top;
            int w = (contenedor != null ? contenedor.ClientSize.Width :
                    (this.ClientSize.Width > 0 ? this.ClientSize.Width : 300));
            tb.Width = w;
            tb.MinimumSize = new Size(w, 28);

            tb.Multiline = true;
            tb.WordWrap = true;
            tb.AcceptsReturn = true;
            tb.ScrollBars = ScrollBars.None;
            tb.ReadOnly = true;
            tb.Enabled = true;
            tb.Cursor = Cursors.Hand;
            tb.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;

            tb.TextChanged += (s, e) => RecalcAutoGrowSafe();

            // Click: marca bloque y selecciona el item
            tb.Click += (s, e) =>
            {
                _encabezadoActivo = false;
                _bloqueSeleccionado = (BloqueTexto)((Control)s).Tag;
                LineaSelection.Select(this, true);
            };

            // Doble clic: editor de notas por bloque
            tb.DoubleClick += (s, e) =>
            {
                _encabezadoActivo = false;
                _bloqueSeleccionado = (BloqueTexto)((Control)s).Tag;
                EditarNotasSeleccionadas(this.FindForm());
            };

            return tb;
        }

        private static string KeyDe(string descripcion, decimal precio)
        {
            string d = (descripcion ?? "").Trim().ToUpperInvariant();
            return d + "|" + precio.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static IEnumerable<string> ToNotas(string notas)
        {
            string t = (notas ?? "").Replace("\r\n", "\n").Replace("\r", "\n");
            string[] arr = t.Split('\n');
            for (int i = 0; i < arr.Length; i++)
            {
                string s = (arr[i] ?? "").Trim();
                if (s.Length == 0) continue;
                if (s.StartsWith("-")) s = s.TrimStart('-').Trim();
                yield return s;
            }
        }

        private static string TextoDe(BloqueTexto b)
        {
            var sb = new StringBuilder();

            if (b.Cantidad <= 1) sb.Append("1 x ").Append(b.Descripcion);
            else sb.Append(b.Cantidad).Append(" x ").Append(b.Descripcion);

            decimal totalExtra = b.PrecioExtra * b.Cantidad;
            if (totalExtra > 0m)
                sb.Append(" = S/ ").Append(totalExtra.ToString("0.00", CultureInfo.InvariantCulture));

            foreach (string n in b.Notas)
                sb.AppendLine().Append("  - ").Append(n);

            return sb.ToString();
        }

        private static TextBox TryGetInnerTextBox(Control guna2TextBox)
        {
            if (guna2TextBox == null) return null;
            try
            {
                PropertyInfo prop = guna2TextBox.GetType().GetProperty(
                    "TextBox", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                return prop != null ? prop.GetValue(guna2TextBox, null) as TextBox : null;
            }
            catch { return null; }
        }

        private static void PrepFlow(FlowLayoutPanel flp)
        {
            if (flp == null) return;
            flp.FlowDirection = FlowDirection.TopDown;
            flp.WrapContents = false;
            flp.AutoSize = true;
            flp.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flp.Dock = DockStyle.Top;
            flp.Margin = new Padding(0);
            flp.Padding = new Padding(0);
        }

        private bool TryFindControl<T>(string name, out T ctrl) where T : Control
        {
            ctrl = this.Controls.Find(name, true).OfType<T>().FirstOrDefault();
            return (ctrl != null);
        }

        private void VincularBloqueVisual(BloqueTexto bloque)
        {
            var tb = bloque.TextBox;
            tb.Tag = bloque;

            tb.Click += (s, e) =>
            {
                _encabezadoActivo = false;
                _bloqueSeleccionado = (BloqueTexto)((Control)s).Tag;
                LineaSelection.Select(this, true);
            };

            tb.DoubleClick += (s, e) =>
            {
                _encabezadoActivo = false;
                _bloqueSeleccionado = (BloqueTexto)((Control)s).Tag;
                EditarNotasSeleccionadas(this.FindForm());
            };
        }

        private static string FormatearNotas(IEnumerable<string> notas)
        {
            List<string> list = (notas ?? new List<string>()).Select(n => n == null ? "" : n.Trim()).Where(n => n.Length > 0).ToList();
            for (int i = 0; i < list.Count; i++)
                if (!list[i].StartsWith("-")) list[i] = "- " + list[i];
            return string.Join(Environment.NewLine, list);
        }

        private static List<string> ParseNotas(string raw)
        {
            return new List<string>(ToNotas(raw));
        }

        public bool EditarNotasSeleccionadas(IWin32Window owner)
        {
            if (_bloqueSeleccionado == null) return false;

            using (var frm = new CapaPresentacion.frmComentarioLbr())
            {
                frm.Texto = FormatearNotas(_bloqueSeleccionado.Notas);
                frm.TextoInicial = frm.Texto;

                if (frm.ShowDialog(owner) == DialogResult.OK)
                {
                    _bloqueSeleccionado.Notas = ParseNotas(frm.Comentario);
                    _bloqueSeleccionado.TextBox.Text = TextoDe(_bloqueSeleccionado);
                    UpdateExportLine(_bloqueSeleccionado);
                    RecalcAutoGrowSafe();
                    return true;
                }
            }
            return false;
        }

        public bool EditarUltimoJugoOBebida(IWin32Window owner)
        {
            BloqueTexto b = _bloqueSeleccionado;
            if (b == null && _ultimoTamal != null) b = _ultimoTamal;
            if (b == null && _ultimoJugo != null) b = _ultimoJugo;
            if (b == null && _ultimaBebida != null) b = _ultimaBebida;
            if (b == null && _tamales.Count > 0) b = _tamales[_tamales.Count - 1];
            if (b == null && _jugos.Count > 0) b = _jugos[_jugos.Count - 1];
            if (b == null && _bebidas.Count > 0) b = _bebidas[_bebidas.Count - 1];

            if (b == null) return false;

            _bloqueSeleccionado = b;
            return EditarNotasSeleccionadas(owner);
        }

        public bool TieneSubItemEditable()
        {
            BloqueTexto candidato = _bloqueSeleccionado;
            if (candidato == null && _ultimoTamal != null) candidato = _ultimoTamal;
            if (candidato == null && _ultimoJugo != null) candidato = _ultimoJugo;
            if (candidato == null && _ultimaBebida != null) candidato = _ultimaBebida;
            if (candidato == null && _tamales.Count > 0) candidato = _tamales[_tamales.Count - 1];
            if (candidato == null && _jugos.Count > 0) candidato = _jugos[_jugos.Count - 1];
            if (candidato == null && _bebidas.Count > 0) candidato = _bebidas[_bebidas.Count - 1];

            return candidato != null;
        }

        private void MarcarEncabezadoActivo()
        {
            _encabezadoActivo = true;
            _bloqueSeleccionado = null;
            LineaSelection.Select(this, true);
        }
    }
}
