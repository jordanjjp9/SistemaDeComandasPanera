namespace CapaPresentacion.ConfiguracionesAdd
{
    partial class frmCambioMesa
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlCambioMesa = new System.Windows.Forms.Panel();
            this.lblDestino = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.lblOrigen = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.pnlDestino = new System.Windows.Forms.Panel();
            this.pnlContDest = new System.Windows.Forms.Panel();
            this.pnlTxtMesaDst = new System.Windows.Forms.Panel();
            this.txtMesaDest = new Guna.UI2.WinForms.Guna2TextBox();
            this.pnlOrigen = new System.Windows.Forms.Panel();
            this.pnlContenidoOrg = new System.Windows.Forms.Panel();
            this.pnlTxtMesaOrg = new System.Windows.Forms.Panel();
            this.txtMesaOrg = new Guna.UI2.WinForms.Guna2TextBox();
            this.pnlCabecera = new System.Windows.Forms.Panel();
            this.lblTitle = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.btnClose = new Guna.UI2.WinForms.Guna2Button();
            this.btnAceptar = new Guna.UI2.WinForms.Guna2Button();
            this.pnlCambioMesa.SuspendLayout();
            this.pnlDestino.SuspendLayout();
            this.pnlTxtMesaDst.SuspendLayout();
            this.pnlOrigen.SuspendLayout();
            this.pnlTxtMesaOrg.SuspendLayout();
            this.pnlCabecera.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlCambioMesa
            // 
            this.pnlCambioMesa.Controls.Add(this.btnAceptar);
            this.pnlCambioMesa.Controls.Add(this.lblDestino);
            this.pnlCambioMesa.Controls.Add(this.lblOrigen);
            this.pnlCambioMesa.Controls.Add(this.pnlDestino);
            this.pnlCambioMesa.Controls.Add(this.pnlOrigen);
            this.pnlCambioMesa.Controls.Add(this.pnlCabecera);
            this.pnlCambioMesa.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCambioMesa.Location = new System.Drawing.Point(0, 0);
            this.pnlCambioMesa.Name = "pnlCambioMesa";
            this.pnlCambioMesa.Size = new System.Drawing.Size(759, 583);
            this.pnlCambioMesa.TabIndex = 0;
            // 
            // lblDestino
            // 
            this.lblDestino.BackColor = System.Drawing.Color.Transparent;
            this.lblDestino.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblDestino.Location = new System.Drawing.Point(478, 77);
            this.lblDestino.Name = "lblDestino";
            this.lblDestino.Size = new System.Drawing.Size(71, 27);
            this.lblDestino.TabIndex = 3;
            this.lblDestino.Text = "Destino";
            // 
            // lblOrigen
            // 
            this.lblOrigen.BackColor = System.Drawing.Color.Transparent;
            this.lblOrigen.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOrigen.Location = new System.Drawing.Point(258, 77);
            this.lblOrigen.Name = "lblOrigen";
            this.lblOrigen.Size = new System.Drawing.Size(64, 27);
            this.lblOrigen.TabIndex = 3;
            this.lblOrigen.Text = "Origen";
            // 
            // pnlDestino
            // 
            this.pnlDestino.Controls.Add(this.pnlContDest);
            this.pnlDestino.Controls.Add(this.pnlTxtMesaDst);
            this.pnlDestino.Location = new System.Drawing.Point(406, 110);
            this.pnlDestino.Name = "pnlDestino";
            this.pnlDestino.Size = new System.Drawing.Size(224, 389);
            this.pnlDestino.TabIndex = 2;
            // 
            // pnlContDest
            // 
            this.pnlContDest.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContDest.Location = new System.Drawing.Point(0, 53);
            this.pnlContDest.Name = "pnlContDest";
            this.pnlContDest.Size = new System.Drawing.Size(224, 336);
            this.pnlContDest.TabIndex = 2;
            // 
            // pnlTxtMesaDst
            // 
            this.pnlTxtMesaDst.Controls.Add(this.txtMesaDest);
            this.pnlTxtMesaDst.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTxtMesaDst.Location = new System.Drawing.Point(0, 0);
            this.pnlTxtMesaDst.Name = "pnlTxtMesaDst";
            this.pnlTxtMesaDst.Size = new System.Drawing.Size(224, 53);
            this.pnlTxtMesaDst.TabIndex = 1;
            // 
            // txtMesaDest
            // 
            this.txtMesaDest.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtMesaDest.DefaultText = "";
            this.txtMesaDest.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtMesaDest.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtMesaDest.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtMesaDest.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtMesaDest.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtMesaDest.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtMesaDest.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtMesaDest.Location = new System.Drawing.Point(72, 9);
            this.txtMesaDest.Name = "txtMesaDest";
            this.txtMesaDest.PlaceholderText = "";
            this.txtMesaDest.SelectedText = "";
            this.txtMesaDest.Size = new System.Drawing.Size(96, 36);
            this.txtMesaDest.TabIndex = 0;
            // 
            // pnlOrigen
            // 
            this.pnlOrigen.Controls.Add(this.pnlContenidoOrg);
            this.pnlOrigen.Controls.Add(this.pnlTxtMesaOrg);
            this.pnlOrigen.Location = new System.Drawing.Point(176, 110);
            this.pnlOrigen.Name = "pnlOrigen";
            this.pnlOrigen.Size = new System.Drawing.Size(224, 389);
            this.pnlOrigen.TabIndex = 1;
            // 
            // pnlContenidoOrg
            // 
            this.pnlContenidoOrg.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlContenidoOrg.Location = new System.Drawing.Point(0, 53);
            this.pnlContenidoOrg.Name = "pnlContenidoOrg";
            this.pnlContenidoOrg.Size = new System.Drawing.Size(224, 336);
            this.pnlContenidoOrg.TabIndex = 1;
            // 
            // pnlTxtMesaOrg
            // 
            this.pnlTxtMesaOrg.Controls.Add(this.txtMesaOrg);
            this.pnlTxtMesaOrg.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTxtMesaOrg.Location = new System.Drawing.Point(0, 0);
            this.pnlTxtMesaOrg.Name = "pnlTxtMesaOrg";
            this.pnlTxtMesaOrg.Size = new System.Drawing.Size(224, 53);
            this.pnlTxtMesaOrg.TabIndex = 0;
            // 
            // txtMesaOrg
            // 
            this.txtMesaOrg.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtMesaOrg.DefaultText = "";
            this.txtMesaOrg.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtMesaOrg.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtMesaOrg.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtMesaOrg.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtMesaOrg.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtMesaOrg.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtMesaOrg.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtMesaOrg.Location = new System.Drawing.Point(72, 9);
            this.txtMesaOrg.Name = "txtMesaOrg";
            this.txtMesaOrg.PlaceholderText = "";
            this.txtMesaOrg.SelectedText = "";
            this.txtMesaOrg.Size = new System.Drawing.Size(96, 36);
            this.txtMesaOrg.TabIndex = 0;
            // 
            // pnlCabecera
            // 
            this.pnlCabecera.Controls.Add(this.lblTitle);
            this.pnlCabecera.Controls.Add(this.btnClose);
            this.pnlCabecera.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCabecera.Location = new System.Drawing.Point(0, 0);
            this.pnlCabecera.Name = "pnlCabecera";
            this.pnlCabecera.Size = new System.Drawing.Size(759, 56);
            this.pnlCabecera.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(312, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(192, 34);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "Cambio de Mesa";
            // 
            // btnClose
            // 
            this.btnClose.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnClose.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnClose.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnClose.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(681, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(78, 56);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "X";
            // 
            // btnAceptar
            // 
            this.btnAceptar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnAceptar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnAceptar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnAceptar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnAceptar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAceptar.ForeColor = System.Drawing.Color.White;
            this.btnAceptar.Location = new System.Drawing.Point(312, 516);
            this.btnAceptar.Name = "btnAceptar";
            this.btnAceptar.Size = new System.Drawing.Size(180, 45);
            this.btnAceptar.TabIndex = 4;
            this.btnAceptar.Text = "Aceptar";
            // 
            // frmCambioMesa
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(759, 583);
            this.Controls.Add(this.pnlCambioMesa);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmCambioMesa";
            this.Text = "frmCambioMesa";
            this.pnlCambioMesa.ResumeLayout(false);
            this.pnlCambioMesa.PerformLayout();
            this.pnlDestino.ResumeLayout(false);
            this.pnlTxtMesaDst.ResumeLayout(false);
            this.pnlOrigen.ResumeLayout(false);
            this.pnlTxtMesaOrg.ResumeLayout(false);
            this.pnlCabecera.ResumeLayout(false);
            this.pnlCabecera.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlCambioMesa;
        private System.Windows.Forms.Panel pnlCabecera;
        private Guna.UI2.WinForms.Guna2Button btnClose;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblDestino;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblOrigen;
        private System.Windows.Forms.Panel pnlDestino;
        private System.Windows.Forms.Panel pnlTxtMesaDst;
        private System.Windows.Forms.Panel pnlOrigen;
        private System.Windows.Forms.Panel pnlTxtMesaOrg;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblTitle;
        private Guna.UI2.WinForms.Guna2TextBox txtMesaDest;
        private Guna.UI2.WinForms.Guna2TextBox txtMesaOrg;
        private System.Windows.Forms.Panel pnlContDest;
        private System.Windows.Forms.Panel pnlContenidoOrg;
        private Guna.UI2.WinForms.Guna2Button btnAceptar;
    }
}