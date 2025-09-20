namespace CapaPresentacion.Administrador
{
    partial class frmAddOn
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
            this.pnlAddOn = new System.Windows.Forms.Panel();
            this.btnAnularDoc = new Guna.UI2.WinForms.Guna2Button();
            this.btnCambioMs = new Guna.UI2.WinForms.Guna2Button();
            this.btnComentarioLrb = new Guna.UI2.WinForms.Guna2Button();
            this.pnlCabecera = new System.Windows.Forms.Panel();
            this.lblTitle = new Guna.UI2.WinForms.Guna2HtmlLabel();
            this.btnClose = new Guna.UI2.WinForms.Guna2Button();
            this.pnlAddOn.SuspendLayout();
            this.pnlCabecera.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlAddOn
            // 
            this.pnlAddOn.Controls.Add(this.btnAnularDoc);
            this.pnlAddOn.Controls.Add(this.btnCambioMs);
            this.pnlAddOn.Controls.Add(this.btnComentarioLrb);
            this.pnlAddOn.Controls.Add(this.pnlCabecera);
            this.pnlAddOn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlAddOn.Location = new System.Drawing.Point(0, 0);
            this.pnlAddOn.Name = "pnlAddOn";
            this.pnlAddOn.Size = new System.Drawing.Size(571, 219);
            this.pnlAddOn.TabIndex = 0;
            // 
            // btnAnularDoc
            // 
            this.btnAnularDoc.BorderRadius = 12;
            this.btnAnularDoc.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnAnularDoc.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnAnularDoc.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnAnularDoc.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnAnularDoc.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnAnularDoc.ForeColor = System.Drawing.Color.White;
            this.btnAnularDoc.Location = new System.Drawing.Point(201, 141);
            this.btnAnularDoc.Name = "btnAnularDoc";
            this.btnAnularDoc.Size = new System.Drawing.Size(180, 45);
            this.btnAnularDoc.TabIndex = 1;
            this.btnAnularDoc.Text = "Anulacion";
            // 
            // btnCambioMs
            // 
            this.btnCambioMs.BorderRadius = 12;
            this.btnCambioMs.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnCambioMs.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnCambioMs.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnCambioMs.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnCambioMs.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCambioMs.ForeColor = System.Drawing.Color.White;
            this.btnCambioMs.Location = new System.Drawing.Point(361, 75);
            this.btnCambioMs.Name = "btnCambioMs";
            this.btnCambioMs.Size = new System.Drawing.Size(180, 45);
            this.btnCambioMs.TabIndex = 1;
            this.btnCambioMs.Text = "Cambio de Mesa";
            // 
            // btnComentarioLrb
            // 
            this.btnComentarioLrb.BorderRadius = 12;
            this.btnComentarioLrb.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnComentarioLrb.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnComentarioLrb.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnComentarioLrb.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnComentarioLrb.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnComentarioLrb.ForeColor = System.Drawing.Color.White;
            this.btnComentarioLrb.Location = new System.Drawing.Point(41, 75);
            this.btnComentarioLrb.Name = "btnComentarioLrb";
            this.btnComentarioLrb.Size = new System.Drawing.Size(180, 45);
            this.btnComentarioLrb.TabIndex = 1;
            this.btnComentarioLrb.Text = "Comentario Libre";
            this.btnComentarioLrb.Click += new System.EventHandler(this.btnComentarioLrb_Click);
            // 
            // pnlCabecera
            // 
            this.pnlCabecera.Controls.Add(this.btnClose);
            this.pnlCabecera.Controls.Add(this.lblTitle);
            this.pnlCabecera.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCabecera.Location = new System.Drawing.Point(0, 0);
            this.pnlCabecera.Name = "pnlCabecera";
            this.pnlCabecera.Size = new System.Drawing.Size(571, 42);
            this.pnlCabecera.TabIndex = 0;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(243, 3);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(109, 34);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "Opciones";
            // 
            // btnClose
            // 
            this.btnClose.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnClose.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnClose.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnClose.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 12.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(512, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(59, 42);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "X";
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // frmAddOn
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(571, 219);
            this.Controls.Add(this.pnlAddOn);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmAddOn";
            this.Text = "frmAddOn";
            this.pnlAddOn.ResumeLayout(false);
            this.pnlCabecera.ResumeLayout(false);
            this.pnlCabecera.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlAddOn;
        private System.Windows.Forms.Panel pnlCabecera;
        private Guna.UI2.WinForms.Guna2HtmlLabel lblTitle;
        private Guna.UI2.WinForms.Guna2Button btnCambioMs;
        private Guna.UI2.WinForms.Guna2Button btnComentarioLrb;
        private Guna.UI2.WinForms.Guna2Button btnAnularDoc;
        private Guna.UI2.WinForms.Guna2Button btnClose;
    }
}