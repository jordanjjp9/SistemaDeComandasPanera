namespace CapaPresentacion
{
    partial class frmMesas
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
            this.pnlMesas = new System.Windows.Forms.Panel();
            this.pnlMCentral = new System.Windows.Forms.Panel();
            this.pnlTop = new System.Windows.Forms.Panel();
            this.pnlButons = new System.Windows.Forms.Panel();
            this.btnSalon = new Guna.UI2.WinForms.Guna2Button();
            this.btnRappi = new System.Windows.Forms.Button();
            this.btnDelivery = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlMesas.SuspendLayout();
            this.pnlTop.SuspendLayout();
            this.pnlButons.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlMesas
            // 
            this.pnlMesas.Controls.Add(this.pnlMCentral);
            this.pnlMesas.Controls.Add(this.pnlTop);
            this.pnlMesas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMesas.Location = new System.Drawing.Point(0, 0);
            this.pnlMesas.Name = "pnlMesas";
            this.pnlMesas.Size = new System.Drawing.Size(1155, 1100);
            this.pnlMesas.TabIndex = 0;
            // 
            // pnlMCentral
            // 
            this.pnlMCentral.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(239)))), ((int)(((byte)(228)))), ((int)(((byte)(214)))));
            this.pnlMCentral.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMCentral.Location = new System.Drawing.Point(0, 53);
            this.pnlMCentral.Name = "pnlMCentral";
            this.pnlMCentral.Size = new System.Drawing.Size(1155, 1047);
            this.pnlMCentral.TabIndex = 1;
            // 
            // pnlTop
            // 
            this.pnlTop.Controls.Add(this.pnlButons);
            this.pnlTop.Controls.Add(this.btnClose);
            this.pnlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Name = "pnlTop";
            this.pnlTop.Size = new System.Drawing.Size(1155, 53);
            this.pnlTop.TabIndex = 0;
            this.pnlTop.SizeChanged += new System.EventHandler(this.pnlTop_SizeChanged);
            // 
            // pnlButons
            // 
            this.pnlButons.Controls.Add(this.btnSalon);
            this.pnlButons.Controls.Add(this.btnRappi);
            this.pnlButons.Controls.Add(this.btnDelivery);
            this.pnlButons.Location = new System.Drawing.Point(304, 3);
            this.pnlButons.Name = "pnlButons";
            this.pnlButons.Size = new System.Drawing.Size(563, 50);
            this.pnlButons.TabIndex = 4;
            // 
            // btnSalon
            // 
            this.btnSalon.BorderRadius = 12;
            this.btnSalon.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnSalon.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnSalon.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnSalon.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnSalon.FillColor = System.Drawing.Color.DarkGray;
            this.btnSalon.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSalon.ForeColor = System.Drawing.Color.White;
            this.btnSalon.Location = new System.Drawing.Point(2, 3);
            this.btnSalon.Name = "btnSalon";
            this.btnSalon.Size = new System.Drawing.Size(101, 45);
            this.btnSalon.TabIndex = 4;
            this.btnSalon.Text = "SALON";
            this.btnSalon.Click += new System.EventHandler(this.btnSalon_Click);
            // 
            // btnRappi
            // 
            this.btnRappi.Location = new System.Drawing.Point(475, 5);
            this.btnRappi.Name = "btnRappi";
            this.btnRappi.Size = new System.Drawing.Size(85, 42);
            this.btnRappi.TabIndex = 3;
            this.btnRappi.Text = "RAPPI";
            this.btnRappi.UseVisualStyleBackColor = true;
            this.btnRappi.Click += new System.EventHandler(this.btnRappi_Click);
            // 
            // btnDelivery
            // 
            this.btnDelivery.Location = new System.Drawing.Point(242, 3);
            this.btnDelivery.Name = "btnDelivery";
            this.btnDelivery.Size = new System.Drawing.Size(85, 42);
            this.btnDelivery.TabIndex = 2;
            this.btnDelivery.Text = "DELIVERY";
            this.btnDelivery.UseVisualStyleBackColor = true;
            this.btnDelivery.Click += new System.EventHandler(this.btnDelivery_Click);
            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.Location = new System.Drawing.Point(1096, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(59, 53);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "X";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // frmMesas
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1155, 1100);
            this.Controls.Add(this.pnlMesas);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmMesas";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "frmMesas";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.frmMesas_Load);
            this.pnlMesas.ResumeLayout(false);
            this.pnlTop.ResumeLayout(false);
            this.pnlButons.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlMesas;
        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Panel pnlMCentral;
        private System.Windows.Forms.Button btnRappi;
        private System.Windows.Forms.Button btnDelivery;
        private System.Windows.Forms.Panel pnlButons;
        private Guna.UI2.WinForms.Guna2Button btnSalon;
    }
}