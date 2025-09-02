namespace CapaPresentacion
{
    partial class frmNumeroPersonas
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
            this.pnlNPersonas = new System.Windows.Forms.Panel();
            this.btnAceptar = new Guna.UI2.WinForms.Guna2Button();
            this.txtCantPers = new Guna.UI2.WinForms.Guna2TextBox();
            this.pnlCabecera = new System.Windows.Forms.Panel();
            this.lblTitle = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.pnlNPersonas.SuspendLayout();
            this.pnlCabecera.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlNPersonas
            // 
            this.pnlNPersonas.Controls.Add(this.btnAceptar);
            this.pnlNPersonas.Controls.Add(this.txtCantPers);
            this.pnlNPersonas.Controls.Add(this.pnlCabecera);
            this.pnlNPersonas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlNPersonas.Location = new System.Drawing.Point(0, 0);
            this.pnlNPersonas.Name = "pnlNPersonas";
            this.pnlNPersonas.Size = new System.Drawing.Size(347, 173);
            this.pnlNPersonas.TabIndex = 0;
            // 
            // btnAceptar
            // 
            this.btnAceptar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnAceptar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnAceptar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnAceptar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnAceptar.FillColor = System.Drawing.Color.Transparent;
            this.btnAceptar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAceptar.ForeColor = System.Drawing.Color.White;
            this.btnAceptar.Image = global::CapaPresentacion.Properties.Resources.check;
            this.btnAceptar.ImageSize = new System.Drawing.Size(40, 40);
            this.btnAceptar.Location = new System.Drawing.Point(236, 82);
            this.btnAceptar.Name = "btnAceptar";
            this.btnAceptar.Size = new System.Drawing.Size(50, 50);
            this.btnAceptar.TabIndex = 2;
            this.btnAceptar.Click += new System.EventHandler(this.btnAceptar_Click);
            // 
            // txtCantPers
            // 
            this.txtCantPers.BorderRadius = 12;
            this.txtCantPers.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtCantPers.DefaultText = "";
            this.txtCantPers.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtCantPers.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtCantPers.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtCantPers.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtCantPers.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtCantPers.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCantPers.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(92)))), ((int)(((byte)(93)))), ((int)(((byte)(88)))));
            this.txtCantPers.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtCantPers.Location = new System.Drawing.Point(138, 82);
            this.txtCantPers.Name = "txtCantPers";
            this.txtCantPers.PlaceholderText = "";
            this.txtCantPers.SelectedText = "";
            this.txtCantPers.Size = new System.Drawing.Size(67, 45);
            this.txtCantPers.TabIndex = 1;
            // 
            // pnlCabecera
            // 
            this.pnlCabecera.Controls.Add(this.lblTitle);
            this.pnlCabecera.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCabecera.Location = new System.Drawing.Point(0, 0);
            this.pnlCabecera.Name = "pnlCabecera";
            this.pnlCabecera.Size = new System.Drawing.Size(347, 53);
            this.pnlCabecera.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(151)))), ((int)(((byte)(145)))));
            this.lblTitle.Location = new System.Drawing.Point(82, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(195, 27);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "Cantidad de Personas";
            // 
            // frmNumeroPersonas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(228)))), ((int)(((byte)(214)))));
            this.ClientSize = new System.Drawing.Size(347, 173);
            this.Controls.Add(this.pnlNPersonas);
            this.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(92)))), ((int)(((byte)(93)))), ((int)(((byte)(88)))));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmNumeroPersonas";
            this.Text = "frmNumeroPersonas";
            this.pnlNPersonas.ResumeLayout(false);
            this.pnlCabecera.ResumeLayout(false);
            this.pnlCabecera.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlNPersonas;
        private System.Windows.Forms.Panel pnlCabecera;
        private Guna.UI2.WinForms.Guna2Button btnAceptar;
        private Guna.UI2.WinForms.Guna2TextBox txtCantPers;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblTitle;
    }
}