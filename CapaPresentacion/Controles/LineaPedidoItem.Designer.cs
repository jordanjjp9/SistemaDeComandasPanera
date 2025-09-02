namespace CapaPresentacion.Controles
{
    partial class LineaPedidoItem
    {
        /// <summary> 
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de componentes

        /// <summary> 
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlPed = new System.Windows.Forms.Panel();
            this.txtNote = new Guna.UI2.WinForms.Guna2TextBox();
            this.txtProducto = new Guna.UI2.WinForms.Guna2TextBox();
            this.pnlPed.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlPed
            // 
            this.pnlPed.AutoSize = true;
            this.pnlPed.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(228)))), ((int)(((byte)(214)))));
            this.pnlPed.Controls.Add(this.txtNote);
            this.pnlPed.Controls.Add(this.txtProducto);
            this.pnlPed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPed.Location = new System.Drawing.Point(0, 0);
            this.pnlPed.Name = "pnlPed";
            this.pnlPed.Size = new System.Drawing.Size(310, 56);
            this.pnlPed.TabIndex = 0;
            // 
            // txtNote
            // 
            this.txtNote.AcceptsReturn = true;
            this.txtNote.Animated = true;
            this.txtNote.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtNote.DefaultText = "";
            this.txtNote.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtNote.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtNote.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtNote.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtNote.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtNote.Enabled = false;
            this.txtNote.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtNote.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtNote.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtNote.Location = new System.Drawing.Point(0, 36);
            this.txtNote.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtNote.Multiline = true;
            this.txtNote.Name = "txtNote";
            this.txtNote.PlaceholderText = "";
            this.txtNote.SelectedText = "";
            this.txtNote.Size = new System.Drawing.Size(310, 43);
            this.txtNote.TabIndex = 10;
            // 
            // txtProducto
            // 
            this.txtProducto.AcceptsReturn = true;
            this.txtProducto.AutoSize = true;
            this.txtProducto.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.txtProducto.DefaultText = "";
            this.txtProducto.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtProducto.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtProducto.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtProducto.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtProducto.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtProducto.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtProducto.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtProducto.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtProducto.Location = new System.Drawing.Point(0, 0);
            this.txtProducto.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.txtProducto.Name = "txtProducto";
            this.txtProducto.PlaceholderText = "";
            this.txtProducto.ReadOnly = true;
            this.txtProducto.SelectedText = "";
            this.txtProducto.Size = new System.Drawing.Size(310, 36);
            this.txtProducto.TabIndex = 8;
            this.txtProducto.TabStop = false;
            // 
            // LineaPedidoItem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoScroll = true;
            this.Controls.Add(this.pnlPed);
            this.Name = "LineaPedidoItem";
            this.Size = new System.Drawing.Size(310, 56);
            this.pnlPed.ResumeLayout(false);
            this.pnlPed.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlPed;
        private Guna.UI2.WinForms.Guna2TextBox txtNote;
        private Guna.UI2.WinForms.Guna2TextBox txtProducto;
    }
}
