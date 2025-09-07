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
            this.txtProducto = new Guna.UI2.WinForms.Guna2TextBox();
            this.pnlPed.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlPed
            // 
            this.pnlPed.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(228)))), ((int)(((byte)(214)))));
            this.pnlPed.Controls.Add(this.txtProducto);
            this.pnlPed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPed.Location = new System.Drawing.Point(0, 0);
            this.pnlPed.Name = "pnlPed";
            this.pnlPed.Size = new System.Drawing.Size(310, 41);
            this.pnlPed.TabIndex = 0;
            // 
            // txtProducto
            // 
            this.txtProducto.AcceptsReturn = true;
            this.txtProducto.BorderRadius = 12;
            this.txtProducto.Cursor = System.Windows.Forms.Cursors.Hand;
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
            this.txtProducto.Multiline = true;
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
            this.Controls.Add(this.pnlPed);
            this.Name = "LineaPedidoItem";
            this.Size = new System.Drawing.Size(310, 41);
            this.pnlPed.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlPed;
        private Guna.UI2.WinForms.Guna2TextBox txtProducto;
    }
}
