namespace CapaPresentacion.Administrador
{
    partial class frmCodigoUser
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
            this.pnlCabecera = new System.Windows.Forms.Panel();
            this.btnCerrar = new Guna.UI2.WinForms.Guna2Button();
            this.guna2HtmlLabel1 = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.txtIngCod = new Guna.UI2.WinForms.Guna2TextBox();
            this.btnAceptar = new Guna.UI2.WinForms.Guna2Button();
            this.pnlCabecera.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlCabecera
            // 
            this.pnlCabecera.Controls.Add(this.btnCerrar);
            this.pnlCabecera.Controls.Add(this.guna2HtmlLabel1);
            this.pnlCabecera.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCabecera.Location = new System.Drawing.Point(0, 0);
            this.pnlCabecera.Name = "pnlCabecera";
            this.pnlCabecera.Size = new System.Drawing.Size(429, 33);
            this.pnlCabecera.TabIndex = 2;
            // 
            // btnCerrar
            // 
            this.btnCerrar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnCerrar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnCerrar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnCerrar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnCerrar.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCerrar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnCerrar.ForeColor = System.Drawing.Color.White;
            this.btnCerrar.Location = new System.Drawing.Point(374, 0);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(55, 33);
            this.btnCerrar.TabIndex = 1;
            this.btnCerrar.Text = "X";
            this.btnCerrar.Click += new System.EventHandler(this.btnCerrar_Click);
            // 
            // guna2HtmlLabel1
            // 
            this.guna2HtmlLabel1.BackColor = System.Drawing.Color.Transparent;
            this.guna2HtmlLabel1.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guna2HtmlLabel1.Location = new System.Drawing.Point(157, 7);
            this.guna2HtmlLabel1.Name = "guna2HtmlLabel1";
            this.guna2HtmlLabel1.Size = new System.Drawing.Size(124, 23);
            this.guna2HtmlLabel1.TabIndex = 0;
            this.guna2HtmlLabel1.Text = "Ingresar Codigo";
            // 
            // txtIngCod
            // 
            this.txtIngCod.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtIngCod.DefaultText = "";
            this.txtIngCod.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtIngCod.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtIngCod.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtIngCod.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtIngCod.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtIngCod.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtIngCod.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtIngCod.Location = new System.Drawing.Point(89, 51);
            this.txtIngCod.Name = "txtIngCod";
            this.txtIngCod.PlaceholderText = "";
            this.txtIngCod.SelectedText = "";
            this.txtIngCod.Size = new System.Drawing.Size(238, 36);
            this.txtIngCod.TabIndex = 3;
            this.txtIngCod.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.txtIngCod_KeyPress);
            // 
            // btnAceptar
            // 
            this.btnAceptar.BorderRadius = 12;
            this.btnAceptar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnAceptar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnAceptar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnAceptar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnAceptar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAceptar.ForeColor = System.Drawing.Color.White;
            this.btnAceptar.Location = new System.Drawing.Point(355, 51);
            this.btnAceptar.Name = "btnAceptar";
            this.btnAceptar.Size = new System.Drawing.Size(53, 36);
            this.btnAceptar.TabIndex = 4;
            this.btnAceptar.Click += new System.EventHandler(this.btnAceptar_Click);
            // 
            // frmCodigoUser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(429, 112);
            this.Controls.Add(this.btnAceptar);
            this.Controls.Add(this.txtIngCod);
            this.Controls.Add(this.pnlCabecera);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmCodigoUser";
            this.Text = "frmCodigoUser";
            this.Shown += new System.EventHandler(this.frmCodigoUser_Shown);
            this.pnlCabecera.ResumeLayout(false);
            this.pnlCabecera.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlCabecera;
        private Guna.UI2.WinForms.Guna2HtmlLabel guna2HtmlLabel1;
        private Guna.UI2.WinForms.Guna2TextBox txtIngCod;
        private Guna.UI2.WinForms.Guna2Button btnAceptar;
        private Guna.UI2.WinForms.Guna2Button btnCerrar;
    }
}