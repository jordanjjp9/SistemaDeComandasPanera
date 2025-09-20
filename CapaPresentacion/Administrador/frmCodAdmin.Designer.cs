namespace CapaPresentacion.Administrador
{
    partial class frmCodAdmin
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
            this.pnlCodAdm = new System.Windows.Forms.Panel();
            this.btnAceptar = new Guna.UI2.WinForms.Guna2Button();
            this.txtCodAdm = new Guna.UI2.WinForms.Guna2TextBox();
            this.pnlCabecera = new System.Windows.Forms.Panel();
            this.guna2HtmlLabel1 = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.pnlCodAdm.SuspendLayout();
            this.pnlCabecera.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlCodAdm
            // 
            this.pnlCodAdm.Controls.Add(this.btnAceptar);
            this.pnlCodAdm.Controls.Add(this.txtCodAdm);
            this.pnlCodAdm.Controls.Add(this.pnlCabecera);
            this.pnlCodAdm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCodAdm.Location = new System.Drawing.Point(0, 0);
            this.pnlCodAdm.Name = "pnlCodAdm";
            this.pnlCodAdm.Size = new System.Drawing.Size(329, 178);
            this.pnlCodAdm.TabIndex = 0;
            // 
            // btnAceptar
            // 
            this.btnAceptar.BorderRadius = 8;
            this.btnAceptar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnAceptar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnAceptar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnAceptar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnAceptar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAceptar.ForeColor = System.Drawing.Color.White;
            this.btnAceptar.Location = new System.Drawing.Point(243, 74);
            this.btnAceptar.Name = "btnAceptar";
            this.btnAceptar.Size = new System.Drawing.Size(52, 36);
            this.btnAceptar.TabIndex = 2;
            this.btnAceptar.Click += new System.EventHandler(this.btnAceptar_Click);
            // 
            // txtCodAdm
            // 
            this.txtCodAdm.BorderRadius = 10;
            this.txtCodAdm.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtCodAdm.DefaultText = "";
            this.txtCodAdm.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtCodAdm.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtCodAdm.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtCodAdm.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtCodAdm.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtCodAdm.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtCodAdm.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtCodAdm.Location = new System.Drawing.Point(61, 74);
            this.txtCodAdm.Name = "txtCodAdm";
            this.txtCodAdm.PlaceholderText = "";
            this.txtCodAdm.SelectedText = "";
            this.txtCodAdm.Size = new System.Drawing.Size(174, 36);
            this.txtCodAdm.TabIndex = 1;
            // 
            // pnlCabecera
            // 
            this.pnlCabecera.Controls.Add(this.guna2HtmlLabel1);
            this.pnlCabecera.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCabecera.Location = new System.Drawing.Point(0, 0);
            this.pnlCabecera.Name = "pnlCabecera";
            this.pnlCabecera.Size = new System.Drawing.Size(329, 40);
            this.pnlCabecera.TabIndex = 0;
            // 
            // guna2HtmlLabel1
            // 
            this.guna2HtmlLabel1.BackColor = System.Drawing.Color.Transparent;
            this.guna2HtmlLabel1.Font = new System.Drawing.Font("Segoe UI Semibold", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guna2HtmlLabel1.Location = new System.Drawing.Point(61, 5);
            this.guna2HtmlLabel1.Name = "guna2HtmlLabel1";
            this.guna2HtmlLabel1.Size = new System.Drawing.Size(218, 27);
            this.guna2HtmlLabel1.TabIndex = 0;
            this.guna2HtmlLabel1.Text = "Codigo de Administrador";
            // 
            // frmCodAdmin
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(329, 178);
            this.Controls.Add(this.pnlCodAdm);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmCodAdmin";
            this.Text = "frmCodAdmin";
            this.Shown += new System.EventHandler(this.frmCodAdmin_Shown);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.frmCodAdmin_KeyDown);
            this.pnlCodAdm.ResumeLayout(false);
            this.pnlCabecera.ResumeLayout(false);
            this.pnlCabecera.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlCodAdm;
        private System.Windows.Forms.Panel pnlCabecera;
        private Guna.UI2.WinForms.Guna2HtmlLabel guna2HtmlLabel1;
        private Guna.UI2.WinForms.Guna2Button btnAceptar;
        private Guna.UI2.WinForms.Guna2TextBox txtCodAdm;
    }
}