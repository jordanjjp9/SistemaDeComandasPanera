namespace CapaPresentacion
{
    partial class ucLineaPedido
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
            this.txtProd = new System.Windows.Forms.TextBox();
            this.txtNot = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // txtProd
            // 
            this.txtProd.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtProd.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtProd.Location = new System.Drawing.Point(0, 0);
            this.txtProd.Multiline = true;
            this.txtProd.Name = "txtProd";
            this.txtProd.Size = new System.Drawing.Size(310, 43);
            this.txtProd.TabIndex = 0;
            // 
            // txtNot
            // 
            this.txtNot.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtNot.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtNot.Location = new System.Drawing.Point(0, 43);
            this.txtNot.Multiline = true;
            this.txtNot.Name = "txtNot";
            this.txtNot.Size = new System.Drawing.Size(310, 34);
            this.txtNot.TabIndex = 1;
            // 
            // ucLineaPedido
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.AppWorkspace;
            this.Controls.Add(this.txtNot);
            this.Controls.Add(this.txtProd);
            this.Name = "ucLineaPedido";
            this.Size = new System.Drawing.Size(310, 134);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtProd;
        private System.Windows.Forms.TextBox txtNot;
    }
}
