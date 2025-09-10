namespace CapaPresentacion.Administrador
{
    partial class frmPrincipal
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
            this.pnlPrincipal = new System.Windows.Forms.Panel();
            this.pnlCentral = new System.Windows.Forms.Panel();
            this.pnlSlideBar = new System.Windows.Forms.Panel();
            this.btnCodigos = new Guna.UI2.WinForms.Guna2Button();
            this.btnImpresion = new Guna.UI2.WinForms.Guna2Button();
            this.pnlCabecera = new System.Windows.Forms.Panel();
            this.btnDespl = new Guna.UI2.WinForms.Guna2Button();
            this.btnCerrar = new Guna.UI2.WinForms.Guna2Button();
            this.guna2HtmlLabel1 = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.pnlPrincipal.SuspendLayout();
            this.pnlSlideBar.SuspendLayout();
            this.pnlCabecera.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlPrincipal
            // 
            this.pnlPrincipal.Controls.Add(this.pnlCentral);
            this.pnlPrincipal.Controls.Add(this.pnlSlideBar);
            this.pnlPrincipal.Controls.Add(this.pnlCabecera);
            this.pnlPrincipal.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlPrincipal.Location = new System.Drawing.Point(0, 0);
            this.pnlPrincipal.Name = "pnlPrincipal";
            this.pnlPrincipal.Size = new System.Drawing.Size(923, 590);
            this.pnlPrincipal.TabIndex = 0;
            // 
            // pnlCentral
            // 
            this.pnlCentral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCentral.Location = new System.Drawing.Point(194, 52);
            this.pnlCentral.Name = "pnlCentral";
            this.pnlCentral.Size = new System.Drawing.Size(729, 538);
            this.pnlCentral.TabIndex = 2;
            // 
            // pnlSlideBar
            // 
            this.pnlSlideBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(1)))), ((int)(((byte)(145)))), ((int)(((byte)(156)))));
            this.pnlSlideBar.Controls.Add(this.btnCodigos);
            this.pnlSlideBar.Controls.Add(this.btnImpresion);
            this.pnlSlideBar.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlSlideBar.Location = new System.Drawing.Point(0, 52);
            this.pnlSlideBar.Name = "pnlSlideBar";
            this.pnlSlideBar.Size = new System.Drawing.Size(194, 538);
            this.pnlSlideBar.TabIndex = 1;
            // 
            // btnCodigos
            // 
            this.btnCodigos.BorderRadius = 20;
            this.btnCodigos.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnCodigos.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnCodigos.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnCodigos.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnCodigos.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnCodigos.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCodigos.ForeColor = System.Drawing.Color.White;
            this.btnCodigos.Location = new System.Drawing.Point(0, 56);
            this.btnCodigos.Name = "btnCodigos";
            this.btnCodigos.Size = new System.Drawing.Size(194, 56);
            this.btnCodigos.TabIndex = 1;
            this.btnCodigos.Text = "Cambio de Codigos";
            this.btnCodigos.Click += new System.EventHandler(this.btnCodigos_Click);
            // 
            // btnImpresion
            // 
            this.btnImpresion.BorderRadius = 20;
            this.btnImpresion.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnImpresion.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnImpresion.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnImpresion.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnImpresion.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnImpresion.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnImpresion.ForeColor = System.Drawing.Color.White;
            this.btnImpresion.Location = new System.Drawing.Point(0, 0);
            this.btnImpresion.Name = "btnImpresion";
            this.btnImpresion.Size = new System.Drawing.Size(194, 56);
            this.btnImpresion.TabIndex = 0;
            this.btnImpresion.Text = "Impresoras";
            this.btnImpresion.Click += new System.EventHandler(this.btnImpresion_Click);
            // 
            // pnlCabecera
            // 
            this.pnlCabecera.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(228)))), ((int)(((byte)(214)))));
            this.pnlCabecera.Controls.Add(this.btnDespl);
            this.pnlCabecera.Controls.Add(this.btnCerrar);
            this.pnlCabecera.Controls.Add(this.guna2HtmlLabel1);
            this.pnlCabecera.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCabecera.Location = new System.Drawing.Point(0, 0);
            this.pnlCabecera.Name = "pnlCabecera";
            this.pnlCabecera.Size = new System.Drawing.Size(923, 52);
            this.pnlCabecera.TabIndex = 0;
            // 
            // btnDespl
            // 
            this.btnDespl.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnDespl.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnDespl.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnDespl.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnDespl.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnDespl.FillColor = System.Drawing.Color.Transparent;
            this.btnDespl.Font = new System.Drawing.Font("Segoe UI Black", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnDespl.ForeColor = System.Drawing.Color.White;
            this.btnDespl.Image = global::CapaPresentacion.Properties.Resources.menu;
            this.btnDespl.Location = new System.Drawing.Point(0, 0);
            this.btnDespl.Name = "btnDespl";
            this.btnDespl.Size = new System.Drawing.Size(60, 52);
            this.btnDespl.TabIndex = 2;
            this.btnDespl.Click += new System.EventHandler(this.btnDespl_Click);
            // 
            // btnCerrar
            // 
            this.btnCerrar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnCerrar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnCerrar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnCerrar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnCerrar.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCerrar.FillColor = System.Drawing.Color.Transparent;
            this.btnCerrar.Font = new System.Drawing.Font("Segoe UI Black", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCerrar.ForeColor = System.Drawing.Color.Black;
            this.btnCerrar.Location = new System.Drawing.Point(848, 0);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(75, 52);
            this.btnCerrar.TabIndex = 1;
            this.btnCerrar.Text = "X";
            this.btnCerrar.Click += new System.EventHandler(this.btnCerrar_Click);
            // 
            // guna2HtmlLabel1
            // 
            this.guna2HtmlLabel1.BackColor = System.Drawing.Color.Transparent;
            this.guna2HtmlLabel1.Font = new System.Drawing.Font("Segoe UI", 20.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.guna2HtmlLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(1)))), ((int)(((byte)(145)))), ((int)(((byte)(156)))));
            this.guna2HtmlLabel1.Location = new System.Drawing.Point(389, 7);
            this.guna2HtmlLabel1.Name = "guna2HtmlLabel1";
            this.guna2HtmlLabel1.Size = new System.Drawing.Size(190, 39);
            this.guna2HtmlLabel1.TabIndex = 0;
            this.guna2HtmlLabel1.Text = "Administrador";
            // 
            // frmPrincipal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(923, 590);
            this.Controls.Add(this.pnlPrincipal);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmPrincipal";
            this.Text = "frmPrincipal";
            this.Load += new System.EventHandler(this.frmPrincipal_Load);
            this.pnlPrincipal.ResumeLayout(false);
            this.pnlSlideBar.ResumeLayout(false);
            this.pnlCabecera.ResumeLayout(false);
            this.pnlCabecera.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlPrincipal;
        private System.Windows.Forms.Panel pnlCabecera;
        private Guna.UI2.WinForms.Guna2HtmlLabel guna2HtmlLabel1;
        private System.Windows.Forms.Panel pnlSlideBar;
        private Guna.UI2.WinForms.Guna2Button btnDespl;
        private Guna.UI2.WinForms.Guna2Button btnCerrar;
        private System.Windows.Forms.Panel pnlCentral;
        private Guna.UI2.WinForms.Guna2Button btnCodigos;
        private Guna.UI2.WinForms.Guna2Button btnImpresion;
    }
}