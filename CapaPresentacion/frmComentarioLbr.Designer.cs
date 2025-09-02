namespace CapaPresentacion
{
    partial class frmComentarioLbr
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
            this.label1 = new System.Windows.Forms.Label();
            this.pnlTopComLbr = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnLlevar = new Guna.UI2.WinForms.Guna2Button();
            this.btnAviso = new Guna.UI2.WinForms.Guna2Button();
            this.btnEnviar = new Guna.UI2.WinForms.Guna2Button();
            this.txtComentLibr = new Guna.UI2.WinForms.Guna2TextBox();
            this.pnlTopComLbr.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(188, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(243, 29);
            this.label1.TabIndex = 0;
            this.label1.Text = "Comentario Libre";
            // 
            // pnlTopComLbr
            // 
            this.pnlTopComLbr.Controls.Add(this.btnClose);
            this.pnlTopComLbr.Controls.Add(this.label1);
            this.pnlTopComLbr.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTopComLbr.Location = new System.Drawing.Point(0, 0);
            this.pnlTopComLbr.Name = "pnlTopComLbr";
            this.pnlTopComLbr.Size = new System.Drawing.Size(607, 49);
            this.pnlTopComLbr.TabIndex = 2;
            // 
            // btnClose
            // 
            this.btnClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnClose.Location = new System.Drawing.Point(539, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(68, 49);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "X";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnLlevar
            // 
            this.btnLlevar.BorderRadius = 12;
            this.btnLlevar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnLlevar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnLlevar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnLlevar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnLlevar.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(151)))), ((int)(((byte)(145)))));
            this.btnLlevar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnLlevar.ForeColor = System.Drawing.Color.White;
            this.btnLlevar.Location = new System.Drawing.Point(137, 230);
            this.btnLlevar.Name = "btnLlevar";
            this.btnLlevar.Size = new System.Drawing.Size(118, 47);
            this.btnLlevar.TabIndex = 4;
            this.btnLlevar.Text = "PARA LLEVAR";
            // 
            // btnAviso
            // 
            this.btnAviso.BorderRadius = 12;
            this.btnAviso.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnAviso.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnAviso.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnAviso.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnAviso.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(151)))), ((int)(((byte)(145)))));
            this.btnAviso.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAviso.ForeColor = System.Drawing.Color.White;
            this.btnAviso.Location = new System.Drawing.Point(361, 230);
            this.btnAviso.Name = "btnAviso";
            this.btnAviso.Size = new System.Drawing.Size(118, 47);
            this.btnAviso.TabIndex = 5;
            this.btnAviso.Text = "AVISO";
            // 
            // btnEnviar
            // 
            this.btnEnviar.BorderRadius = 12;
            this.btnEnviar.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnEnviar.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnEnviar.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnEnviar.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnEnviar.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(154)))), ((int)(((byte)(151)))), ((int)(((byte)(145)))));
            this.btnEnviar.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnEnviar.ForeColor = System.Drawing.Color.White;
            this.btnEnviar.Location = new System.Drawing.Point(515, 116);
            this.btnEnviar.Name = "btnEnviar";
            this.btnEnviar.Size = new System.Drawing.Size(80, 54);
            this.btnEnviar.TabIndex = 6;
            this.btnEnviar.Text = "Enviar";
            this.btnEnviar.Click += new System.EventHandler(this.btnEnviar_Click);
            // 
            // txtComentLibr
            // 
            this.txtComentLibr.AcceptsReturn = true;
            this.txtComentLibr.Animated = true;
            this.txtComentLibr.AutoScroll = true;
            this.txtComentLibr.BorderRadius = 12;
            this.txtComentLibr.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtComentLibr.DefaultText = "";
            this.txtComentLibr.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtComentLibr.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtComentLibr.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtComentLibr.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtComentLibr.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtComentLibr.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.txtComentLibr.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtComentLibr.Location = new System.Drawing.Point(34, 89);
            this.txtComentLibr.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.txtComentLibr.Multiline = true;
            this.txtComentLibr.Name = "txtComentLibr";
            this.txtComentLibr.PlaceholderText = "";
            this.txtComentLibr.ScrollBars = System.Windows.Forms.ScrollBars.Horizontal;
            this.txtComentLibr.SelectedText = "";
            this.txtComentLibr.Size = new System.Drawing.Size(475, 115);
            this.txtComentLibr.TabIndex = 7;
            // 
            // frmComentarioLbr
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(1)))), ((int)(((byte)(145)))), ((int)(((byte)(156)))));
            this.ClientSize = new System.Drawing.Size(607, 307);
            this.Controls.Add(this.txtComentLibr);
            this.Controls.Add(this.btnEnviar);
            this.Controls.Add(this.btnAviso);
            this.Controls.Add(this.btnLlevar);
            this.Controls.Add(this.pnlTopComLbr);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmComentarioLbr";
            this.Text = "frmComentarioLbr";
            this.pnlTopComLbr.ResumeLayout(false);
            this.pnlTopComLbr.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel pnlTopComLbr;
        private System.Windows.Forms.Button btnClose;
        private Guna.UI2.WinForms.Guna2Button btnLlevar;
        private Guna.UI2.WinForms.Guna2Button btnAviso;
        private Guna.UI2.WinForms.Guna2Button btnEnviar;
        private Guna.UI2.WinForms.Guna2TextBox txtComentLibr;
    }
}