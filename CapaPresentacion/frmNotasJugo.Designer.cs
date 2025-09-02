namespace CapaPresentacion
{
    partial class frmNotasJugo
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
            this.pnlNotJug = new System.Windows.Forms.Panel();
            this.btnCali = new System.Windows.Forms.Button();
            this.pnlNotJug.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlNotJug
            // 
            this.pnlNotJug.Controls.Add(this.btnCali);
            this.pnlNotJug.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlNotJug.Location = new System.Drawing.Point(0, 0);
            this.pnlNotJug.Name = "pnlNotJug";
            this.pnlNotJug.Size = new System.Drawing.Size(1249, 511);
            this.pnlNotJug.TabIndex = 0;
            // 
            // btnCali
            // 
            this.btnCali.FlatAppearance.BorderSize = 0;
            this.btnCali.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCali.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnCali.Location = new System.Drawing.Point(12, 12);
            this.btnCali.Name = "btnCali";
            this.btnCali.Size = new System.Drawing.Size(130, 150);
            this.btnCali.TabIndex = 0;
            this.btnCali.Text = "Caliente";
            this.btnCali.UseVisualStyleBackColor = true;
            // 
            // frmNotasJugo
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlDark;
            this.ClientSize = new System.Drawing.Size(1249, 511);
            this.Controls.Add(this.pnlNotJug);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmNotasJugo";
            this.Text = "frmNotasJugo";
            this.pnlNotJug.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlNotJug;
        private System.Windows.Forms.Button btnCali;
    }
}