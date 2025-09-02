namespace CapaPresentacion
{
    partial class frmNotaHelados
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
            this.pnlCentralNHelados = new System.Windows.Forms.Panel();
            this.pnlCabecera = new System.Windows.Forms.Panel();
            this.btnCerrar = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.btnSLechug = new System.Windows.Forms.Button();
            this.pnlCentralNHelados.SuspendLayout();
            this.pnlCabecera.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlCentralNHelados
            // 
            this.pnlCentralNHelados.BackColor = System.Drawing.SystemColors.ControlDark;
            this.pnlCentralNHelados.Controls.Add(this.button2);
            this.pnlCentralNHelados.Controls.Add(this.button1);
            this.pnlCentralNHelados.Controls.Add(this.btnSLechug);
            this.pnlCentralNHelados.Controls.Add(this.pnlCabecera);
            this.pnlCentralNHelados.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlCentralNHelados.Location = new System.Drawing.Point(0, 0);
            this.pnlCentralNHelados.Name = "pnlCentralNHelados";
            this.pnlCentralNHelados.Size = new System.Drawing.Size(1249, 511);
            this.pnlCentralNHelados.TabIndex = 0;
            // 
            // pnlCabecera
            // 
            this.pnlCabecera.Controls.Add(this.btnCerrar);
            this.pnlCabecera.Controls.Add(this.label1);
            this.pnlCabecera.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCabecera.Location = new System.Drawing.Point(0, 0);
            this.pnlCabecera.Name = "pnlCabecera";
            this.pnlCabecera.Size = new System.Drawing.Size(1249, 59);
            this.pnlCabecera.TabIndex = 0;
            // 
            // btnCerrar
            // 
            this.btnCerrar.Dock = System.Windows.Forms.DockStyle.Left;
            this.btnCerrar.Location = new System.Drawing.Point(0, 0);
            this.btnCerrar.Name = "btnCerrar";
            this.btnCerrar.Size = new System.Drawing.Size(86, 59);
            this.btnCerrar.TabIndex = 5;
            this.btnCerrar.Text = "X";
            this.btnCerrar.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Verdana", 26.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(643, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(364, 42);
            this.label1.TabIndex = 4;
            this.label1.Text = "Opciones Bebidas";
            // 
            // button2
            // 
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button2.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button2.Location = new System.Drawing.Point(366, 65);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(171, 162);
            this.button2.TabIndex = 8;
            this.button2.Text = "Para Llevar";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.button1.Location = new System.Drawing.Point(189, 65);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(171, 162);
            this.button1.TabIndex = 7;
            this.button1.Text = "Vaso";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // btnSLechug
            // 
            this.btnSLechug.FlatAppearance.BorderSize = 0;
            this.btnSLechug.Font = new System.Drawing.Font("Verdana", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnSLechug.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnSLechug.Location = new System.Drawing.Point(12, 65);
            this.btnSLechug.Name = "btnSLechug";
            this.btnSLechug.Size = new System.Drawing.Size(171, 162);
            this.btnSLechug.TabIndex = 6;
            this.btnSLechug.Text = "Cono";
            this.btnSLechug.UseVisualStyleBackColor = true;
            // 
            // frmNotaHelados
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1249, 511);
            this.Controls.Add(this.pnlCentralNHelados);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "frmNotaHelados";
            this.Text = "frmNotaHelados";
            this.pnlCentralNHelados.ResumeLayout(false);
            this.pnlCabecera.ResumeLayout(false);
            this.pnlCabecera.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlCentralNHelados;
        private System.Windows.Forms.Panel pnlCabecera;
        private System.Windows.Forms.Button btnCerrar;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnSLechug;
    }
}