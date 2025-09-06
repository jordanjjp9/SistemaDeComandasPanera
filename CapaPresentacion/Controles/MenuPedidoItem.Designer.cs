namespace CapaPresentacion.Controles
{
    partial class MenuPedidoItem
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
            this.pnlMenu = new System.Windows.Forms.Panel();
            this.txtMenu = new Guna.UI2.WinForms.Guna2TextBox();
            this.txtChich = new Guna.UI2.WinForms.Guna2TextBox();
            this.pnlMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlMenu
            // 
            this.pnlMenu.Controls.Add(this.txtChich);
            this.pnlMenu.Controls.Add(this.txtMenu);
            this.pnlMenu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMenu.Location = new System.Drawing.Point(0, 0);
            this.pnlMenu.Name = "pnlMenu";
            this.pnlMenu.Size = new System.Drawing.Size(310, 76);
            this.pnlMenu.TabIndex = 0;
            // 
            // txtMenu
            // 
            this.txtMenu.AcceptsReturn = true;
            this.txtMenu.BorderRadius = 12;
            this.txtMenu.Cursor = System.Windows.Forms.Cursors.Hand;
            this.txtMenu.DefaultText = "";
            this.txtMenu.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtMenu.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtMenu.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtMenu.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtMenu.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtMenu.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtMenu.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtMenu.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtMenu.Location = new System.Drawing.Point(0, 0);
            this.txtMenu.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.txtMenu.Multiline = true;
            this.txtMenu.Name = "txtMenu";
            this.txtMenu.PlaceholderText = "";
            this.txtMenu.ReadOnly = true;
            this.txtMenu.SelectedText = "";
            this.txtMenu.Size = new System.Drawing.Size(310, 36);
            this.txtMenu.TabIndex = 9;
            this.txtMenu.TabStop = false;
            // 
            // txtChich
            // 
            this.txtChich.AcceptsReturn = true;
            this.txtChich.BorderRadius = 12;
            this.txtChich.Cursor = System.Windows.Forms.Cursors.Hand;
            this.txtChich.DefaultText = "";
            this.txtChich.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtChich.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtChich.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtChich.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtChich.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtChich.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtChich.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtChich.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtChich.Location = new System.Drawing.Point(0, 36);
            this.txtChich.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
            this.txtChich.Multiline = true;
            this.txtChich.Name = "txtChich";
            this.txtChich.PlaceholderText = "";
            this.txtChich.ReadOnly = true;
            this.txtChich.SelectedText = "";
            this.txtChich.Size = new System.Drawing.Size(310, 36);
            this.txtChich.TabIndex = 10;
            this.txtChich.TabStop = false;
            // 
            // MenuPedidoItem
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlMenu);
            this.Name = "MenuPedidoItem";
            this.Size = new System.Drawing.Size(310, 76);
            this.pnlMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlMenu;
        private Guna.UI2.WinForms.Guna2TextBox txtChich;
        private Guna.UI2.WinForms.Guna2TextBox txtMenu;
    }
}
